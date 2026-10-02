#!/usr/bin/env python3
"""Render the whole profile as CRT-styled SVGs (stdlib only).

Live data (run in GitHub Actions):
    GITHUB_TOKEN=... python scripts/crt_render.py --user OwainWilliams
Offline preview with deterministic sample data:
    python scripts/crt_render.py --sample

GitHub strips <style>/<script> from READMEs but plays CSS animation inside
<img src="x.svg">, so every effect lives in the SVG files themselves.
Text inside an <img> SVG is not clickable, so each row is its own SVG and the
README wraps it in a normal markdown/HTML link.
"""
import argparse, datetime as dt, html, json, os, random, re, sys, urllib.request
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ASSETS = os.path.join(ROOT, "assets")
README = os.path.join(ROOT, "README.md")
G, DIM, FAINT = "#33ff66", "#1fa645", "#0b3d1a"
FONT = "'Courier New', Courier, monospace"
W = 900

# ---------------------------------------------------------------- shared SVG
CSS = f"""
text{{font-family:{FONT};fill:{G};}}
.flick{{animation:flick 6s infinite steps(1);}}
@keyframes flick{{0%,100%{{opacity:1}}41%{{opacity:.96}}42%{{opacity:.88}}43%{{opacity:1}}77%{{opacity:.95}}78%{{opacity:1}}}}
.roll{{animation:roll .6s linear infinite;}}
@keyframes roll{{from{{transform:translateY(0)}}to{{transform:translateY(4px)}}}}
.sweep{{animation:sweep 7s linear infinite;}}
@keyframes sweep{{from{{transform:translateY(-60px)}}to{{transform:translateY(420px)}}}}
.cur{{animation:blink 1s steps(1) infinite;}}
@keyframes blink{{0%,49%{{opacity:1}}50%,100%{{opacity:0}}}}
@media (prefers-reduced-motion:reduce){{*{{animation:none !important}} .typed{{width:100% !important}}}}
"""

DEFS = f"""
<filter id="glow" x="-5%" y="-30%" width="110%" height="160%">
  <feGaussianBlur stdDeviation="1.6" result="b"/>
  <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
</filter>
<filter id="glow2" x="-50%" y="-50%" width="200%" height="200%">
  <feGaussianBlur stdDeviation="2.4" result="b"/>
  <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
</filter>
<pattern id="scan" width="4" height="4" patternUnits="userSpaceOnUse"><rect width="4" height="2" fill="#000" opacity="0.28"/></pattern>
<radialGradient id="vig" cx="50%" cy="50%" r="75%"><stop offset="55%" stop-color="#000" stop-opacity="0"/><stop offset="100%" stop-color="#000" stop-opacity="0.75"/></radialGradient>
<radialGradient id="phos" cx="50%" cy="45%" r="70%"><stop offset="0%" stop-color="#06210f"/><stop offset="100%" stop-color="#020b05"/></radialGradient>
"""

def esc(s): return html.escape(str(s), quote=True)

def overlay(w, h, rx=6):
    return (f'<g clip-path="url(#scr)" style="pointer-events:none">'
            f'<g class="roll"><rect x="0" y="-8" width="{w}" height="{h+16}" fill="url(#scan)"/></g>'
            f'<rect width="{w}" height="{h}" fill="url(#vig)"/>'
            f'<rect class="sweep" width="{w}" height="40" fill="{G}" opacity="0.05"/></g>')

def panel(w, h, inner, label, extra_css="", extra_defs=""):
    """A flat CRT panel (no bezel) used for every section so they all match."""
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}" role="img" aria-label="{esc(label)}">
<style>{CSS}{extra_css}</style>
<defs>{DEFS}{extra_defs}<clipPath id="scr"><rect width="{w}" height="{h}" rx="6"/></clipPath></defs>
<rect width="{w}" height="{h}" rx="6" fill="#020b05"/>
<rect x="1" y="1" width="{w-2}" height="{h-2}" rx="6" fill="none" stroke="{DIM}" stroke-width="1.5" opacity=".7"/>
<g class="flick">{inner}{overlay(w, h)}</g>
</svg>'''

def write(name, svg):
    os.makedirs(ASSETS, exist_ok=True)
    with open(os.path.join(ASSETS, name), "w", encoding="utf-8") as f: f.write(svg)

def trunc(s, n): return s if len(s) <= n else s[:n-1] + "…"

# ------------------------------------------------------------------ the monitor
def header():
    bx, by, bw, bh = 10, 10, 880, 360
    sx, sy, sw, sh = 34, 34, 832, 312
    H = 380
    lines = ["OWAIN-OS v1.0  (C) 1991 INITIALS LABS", "MEMORY CHECK ........ 640K OK",
             "LOADING PROFILE.SYS ... DONE", "", "C:\\> LOGIN OWAINCODES", "C:\\> TYPE WHOAMI.TXT",
             "   NAME ...... OWAIN WILLIAMS", "   ROLE ...... SENIOR .NET DEVELOPER",
             "   FOCUS ..... UMBRACO / .NET / OSS", "   LOCATION .. SCOTLAND",
             "   STATUS .... OPEN TO COLLABORATE"]
    CYCLE, start, step = 18.0, 0.5, 1.05
    css, body, defs = "", "", ""
    y0, lh = sy + 34, 24
    for i, t in enumerate(lines):
        if not t: continue
        y = y0 + i * lh
        a = (start + i * step) / CYCLE * 100
        b = a + (len(t) * 0.045) / CYCLE * 100
        css += f"@keyframes t{i}{{0%,{a:.2f}%{{width:0}}{b:.2f}%,94%{{width:{sw-40}px}}100%{{width:0}}}}.l{i}{{animation:t{i} {CYCLE}s infinite steps({len(t)});}}\n"
        defs += f'<clipPath id="c{i}"><rect class="typed l{i}" x="{sx+20}" y="{y-18}" width="0" height="{lh}"/></clipPath>'
        body += f'<text x="{sx+20}" y="{y}" font-size="17" clip-path="url(#c{i})" filter="url(#glow)" xml:space="preserve">{esc(t)}</text>'
    cy = y0 + len(lines) * lh
    ca = (start + len(lines) * step) / CYCLE * 100
    css += f"@keyframes cs{{0%,{ca:.2f}%{{opacity:0}}{ca+.1:.2f}%,94%{{opacity:1}}100%{{opacity:0}}}}.curwrap{{animation:cs {CYCLE}s infinite steps(1);}}"
    body += (f'<g class="curwrap"><text x="{sx+20}" y="{cy}" font-size="17" filter="url(#glow)">C:\\&gt;</text>'
             f'<rect class="cur" x="{sx+66}" y="{cy-15}" width="11" height="18" fill="{G}" filter="url(#glow)"/></g>')
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" role="img" aria-label="Retro green-screen terminal: Owain Williams, Senior .NET Developer, Umbraco, Scotland, open to collaborate">
<style>{CSS}{css}</style>
<defs>{DEFS}{defs}<clipPath id="scr"><rect x="{sx}" y="{sy}" width="{sw}" height="{sh}" rx="26"/></clipPath>
<linearGradient id="bez" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#cfc8b4"/><stop offset="1" stop-color="#9d977f"/></linearGradient></defs>
<rect x="{bx}" y="{by}" width="{bw}" height="{bh}" rx="38" fill="url(#bez)" stroke="#6f6a58" stroke-width="3"/>
<rect x="{sx-8}" y="{sy-8}" width="{sw+16}" height="{sh+16}" rx="32" fill="#1b1a16"/>
<rect x="{sx}" y="{sy}" width="{sw}" height="{sh}" rx="26" fill="url(#phos)"/>
<g class="flick">{body}
<g clip-path="url(#scr)" style="pointer-events:none"><g class="roll"><rect x="{sx}" y="{sy-8}" width="{sw}" height="{sh+16}" fill="url(#scan)"/></g>
<rect x="{sx}" y="{sy}" width="{sw}" height="{sh}" fill="url(#vig)"/><rect class="sweep" x="{sx}" y="{sy}" width="{sw}" height="40" fill="{G}" opacity="0.05"/></g></g>
<text x="{bx+44}" y="{H-14}" font-size="11" style="fill:#5d5846;font-family:{FONT}" letter-spacing="3">OWAINTRON 9000</text>
<circle cx="{W-52}" cy="{H-22}" r="4" fill="{G}" class="cur"/>
</svg>'''
    write("crt-header.svg", svg)

def bar(name, label):
    h = 44
    cx = 22 + (len(label) + 2) * 11.4
    inner = (f'<text x="18" y="29" font-size="19" filter="url(#glow)" xml:space="preserve">&gt; {esc(label)}</text>'
             f'<rect class="cur" x="{cx:.0f}" y="12" width="11" height="20" fill="{G}" filter="url(#glow)"/>')
    write(name, panel(W, h, inner, label))

# ------------------------------------------------------------ contribution map
def sample_days(seed=1991):
    rnd = random.Random(seed)
    today = dt.date.today()
    start = today - dt.timedelta(days=today.weekday() + 1 + 52 * 7)  # a Sunday
    days, burst = [], 0
    d = start
    while d <= today:
        wd = d.weekday()
        base = 0.15 if wd >= 5 else 0.62
        if rnd.random() < 0.04: burst = rnd.randint(3, 9)
        p = min(0.95, base + (0.3 if burst else 0))
        n = int(rnd.expovariate(1 / 4.5)) + 1 if rnd.random() < p else 0
        if burst: burst -= 1; n += rnd.randint(2, 9)
        days.append((d.isoformat(), n)); d += dt.timedelta(days=1)
    return days, {"commits": sum(n for _, n in days) , "prs": 87, "issues": 12, "repos": 9}

def fetch_days(user, token):
    q = """query($l:String!){user(login:$l){contributionsCollection{totalCommitContributions
      totalPullRequestContributions totalIssueContributions totalRepositoryContributions
      contributionCalendar{weeks{contributionDays{date contributionCount}}}}}}"""
    req = urllib.request.Request("https://api.github.com/graphql",
        data=json.dumps({"query": q, "variables": {"l": user}}).encode(),
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json", "User-Agent": "crt-render"})
    with urllib.request.urlopen(req, timeout=30) as r: data = json.load(r)
    cc = data["data"]["user"]["contributionsCollection"]
    days = [(d["date"], d["contributionCount"]) for w in cc["contributionCalendar"]["weeks"] for d in w["contributionDays"]]
    return days, {"commits": cc["totalCommitContributions"], "prs": cc["totalPullRequestContributions"],
                  "issues": cc["totalIssueContributions"], "repos": cc["totalRepositoryContributions"]}

def streaks(days):
    best = cur = 0
    for _, n in days:
        cur = cur + 1 if n else 0; best = max(best, cur)
    now = 0
    for _, n in reversed(days):
        if n: now += 1
        elif now == 0 and _ == days[-1][0]: continue   # today may still be empty
        else: break
    return best, now

def contrib(days, totals, sample):
    cell, gap, left, top = 12, 3, 52, 78
    first = dt.date.fromisoformat(days[0][0])
    pad = (first.weekday() + 1) % 7                     # GitHub weeks start Sunday
    cols = (len(days) + pad + 6) // 7
    nz = sorted(n for _, n in days if n)
    q = lambda f: nz[int(len(nz) * f)] if nz else 1
    cuts = (q(.25), q(.5), q(.75))
    lvl = lambda n: 0 if n == 0 else 1 if n <= cuts[0] else 2 if n <= cuts[1] else 3 if n <= cuts[2] else 4
    fill = {0: FAINT, 1: "#0f6b2c", 2: "#17a840", 3: "#2ee860", 4: "#9dffb8"}
    cells, glow_cells, months = [], [], []
    seen = -1
    CYC = 16.0
    for i, (ds, n) in enumerate(days):
        idx = i + pad; c, r = idx // 7, idx % 7
        x, y = left + c * (cell + gap), top + r * (cell + gap)
        d = dt.date.fromisoformat(ds)
        if d.month != seen and r < 3 and d.day <= 21 and (not months or x - months[-1][0] > 40):
            months.append((x, d.strftime("%b").upper())); seen = d.month
        l = lvl(n)
        style = f'style="animation-delay:{c*0.07:.2f}s"'
        rect = f'<rect class="cell" x="{x}" y="{y}" width="{cell}" height="{cell}" rx="1.5" fill="{fill[l]}" {style}><title>{ds}: {n}</title></rect>'
        (glow_cells if l >= 3 else cells).append(rect)
    best, now = streaks(days)
    total = sum(n for _, n in days)
    gw = left + cols * (cell + gap)
    h = top + 7 * (cell + gap) + 74
    inner = f'<text x="18" y="29" font-size="19" filter="url(#glow)">&gt; CAT COMMIT_HISTORY.LOG</text>'
    inner += f'<text x="{W-18}" y="29" font-size="14" text-anchor="end" style="fill:{DIM}">LAST 52 WEEKS{" · SAMPLE DATA" if sample else ""}</text>'
    for x, m in months:
        inner += f'<text x="{x}" y="{top-10}" font-size="11" style="fill:{DIM}">{m}</text>'
    for r, lab in ((1, "MON"), (3, "WED"), (5, "FRI")):
        inner += f'<text x="18" y="{top + r*(cell+gap) + 9}" font-size="10" style="fill:{DIM}">{lab}</text>'
    inner += f'<g>{"".join(cells)}</g><g filter="url(#glow2)">{"".join(glow_cells)}</g>'
    # scan beam that drags across the grid
    inner += f'<rect class="beam" x="{left-4}" y="{top-4}" width="3" height="{7*(cell+gap)+6}" fill="{G}" opacity=".9" filter="url(#glow2)"/>'
    ly = top + 7 * (cell + gap) + 26
    stats = (f"TOTAL {total:,}   COMMITS {totals['commits']:,}   PRS {totals['prs']:,}   "
             f"ISSUES {totals['issues']:,}   LONGEST STREAK {best}D   CURRENT {now}D")
    inner += f'<text x="18" y="{ly}" font-size="14" filter="url(#glow)">{esc(stats)}</text>'
    lx = W - 18 - 5 * 18 - 70
    inner += f'<text x="{lx-8}" y="{ly+34}" font-size="11" text-anchor="end" style="fill:{DIM}">LESS</text>'
    for k in range(5):
        inner += f'<rect x="{lx+k*18}" y="{ly+24}" width="{cell}" height="{cell}" rx="1.5" fill="{fill[k]}"/>'
    inner += f'<text x="{lx+5*18+4}" y="{ly+34}" font-size="11" style="fill:{DIM}">MORE</text>'
    inner += f'<text x="18" y="{ly+34}" font-size="13" style="fill:{DIM}">&gt; </text><rect class="cur" x="34" y="{ly+22}" width="9" height="14" fill="{G}"/>'
    css = (f".cell{{animation:cellin {CYC}s infinite backwards}}"
           f"@keyframes cellin{{0%{{opacity:0}}3%{{opacity:1}}88%{{opacity:1}}100%{{opacity:0}}}}"
           f".beam{{animation:beam {CYC}s infinite linear}}"
           f"@keyframes beam{{0%{{transform:translateX(0);opacity:1}}"
           f"{(cols*0.07/CYC)*100:.1f}%{{transform:translateX({cols*(cell+gap)}px);opacity:1}}"
           f"{(cols*0.07/CYC)*100+.1:.1f}%,100%{{transform:translateX({cols*(cell+gap)}px);opacity:0}}}}")
    write("crt-contrib.svg", panel(W, h, inner, f"Contribution history: {total} contributions in the last year", css))

# -------------------------------------------------------------- data-driven rows
def row(name, left, right, label, w=W, h=34):
    inner = (f'<text x="16" y="23" font-size="16" filter="url(#glow)" xml:space="preserve">{esc(left)}</text>'
             f'<text x="{w-16}" y="23" font-size="14" text-anchor="end" style="fill:{DIM}" xml:space="preserve">{esc(right)}</text>')
    write(name, panel(w, h, inner, label))

SAMPLE_POSTS = [
    ("AI as a learning tool", "https://owain.codes/blog/2026/september/ai-as-a-learning-tool/", "2026-09-28"),
    ("The coffee log, rebuilt in .NET", "https://owain.codes/blog/2026/september/the-coffee-log-rebuilt-in-net/", "2026-09-27"),
    ("Time to reset", "https://owain.codes/blog/2026/september/time-to-reset/", "2026-09-25"),
    ("Initials.Autolink", "https://owain.codes/blog/2026/september/initialsautolink/", "2026-09-03"),
    ("A reusable deploy pipeline for Umbraco Cloud", "https://owain.codes/blog/2026/july/a-reusable-deploy-pipeline-for-umbraco-cloud/", "2026-07-27"),
]
SAMPLE_PKGS = ["Umbraco.Community.MediaColourFinder", "oc.MultipleDatePicker", "OC.PowerSort", "OC.MediaColourFinderV2",
               "OC.UFMFallbacks", "OC.Automate.Mastodon", "OC.Automate.LinkedIn", "OC.MultiHotspot", "OC.Automate.Bluesky",
               "OC.HiddenDashboard", "ocTweetThis", "OC.UFMMemberLookup", "Initials.AutoLink"]

def fetch_posts(sample):
    if not sample:
        try:
            with urllib.request.urlopen("https://owain.codes/rss", timeout=20) as r: root = ET.fromstring(r.read())
            out = []
            for it in root.iter("item"):
                d = dt.datetime.strptime(it.findtext("pubDate")[:16], "%a, %d %b %Y").date()
                out.append((it.findtext("title"), it.findtext("link"), d.isoformat()))
            if out: return out[:5]
        except Exception as e: print("blog feed failed, using sample:", e, file=sys.stderr)
    return SAMPLE_POSTS

def fetch_pkgs(sample):
    if not sample:
        try:
            u = "https://azuresearch-usnc.nuget.org/query?q=owner:scottishcoder&take=50"
            with urllib.request.urlopen(u, timeout=20) as r: data = json.load(r)["data"]
            if data: return [(p["id"], p.get("version", "?"), p.get("totalDownloads", 0)) for p in data]
        except Exception as e: print("nuget failed, using sample:", e, file=sys.stderr)
    rnd = random.Random(7)
    return [(n, f"{rnd.randint(1,3)}.{rnd.randint(0,9)}.{rnd.randint(0,9)}", rnd.randint(300, 40000)) for n in SAMPLE_PKGS]

def kfmt(n): return f"{n/1000:.1f}K" if n >= 1000 else str(n)

def blog_rows(posts):
    md = []
    for i, (t, link, d) in enumerate(posts):
        date = dt.date.fromisoformat(d).strftime("%y/%m/%d")
        row(f"post-{i}.svg", f"{i+1:02d}  {trunc(t.upper(), 62)}", date, t)
        md.append(f'<a href="{esc(link)}"><img src="assets/post-{i}.svg" alt="{esc(t)}" width="900"/></a>')
    return md

def pkg_rows(pkgs):
    md, total = [], sum(p[2] for p in pkgs)
    for i, (name, ver, dl) in enumerate(pkgs):
        dots = "." * max(2, 46 - len(name))
        row(f"pkg-{i}.svg", f"{name} {dots}", f"v{ver}   ↓ {kfmt(dl)}", f"{name} {ver}, {dl} downloads", h=30)
        md.append(f'<a href="https://www.nuget.org/packages/{esc(name)}"><img src="assets/pkg-{i}.svg" alt="{esc(name)}" width="900"/></a>')
    return md, total

def audio(tracks):
    h = 44 + len(tracks) * 30
    inner = f'<text x="18" y="29" font-size="19" filter="url(#glow)">&gt; NOW_PLAYING.WAV</text>'
    css = ""
    for i, (artist, title) in enumerate(tracks):
        y = 64 + i * 30
        inner += f'<text x="54" y="{y}" font-size="15" filter="url(#glow)">{esc(trunc(f"{artist.upper()} - {title.upper()}", 80))}</text>'
        for b in range(4):   # tiny animated equaliser on each line
            inner += f'<rect class="eq e{b}" x="{18+b*8}" y="{y-14}" width="5" height="14" fill="{G}" style="animation-delay:-{(i*4+b)*0.37:.2f}s"/>'
    css = ".eq{transform-box:fill-box;transform-origin:bottom;animation:eq .9s ease-in-out infinite}.e1{animation-duration:.7s}.e2{animation-duration:1.1s}.e3{animation-duration:.8s}@keyframes eq{0%,100%{transform:scaleY(.25)}50%{transform:scaleY(1)}}"
    write("crt-audio.svg", panel(W, h, inner, "Recently played tracks", css))

def fetch_tracks(sample):
    if not sample:
        try:
            with urllib.request.urlopen("https://ws.audioscrobbler.com/1.0/user/owaincodes/recenttracks.rss", timeout=15) as r:
                root = ET.fromstring(r.read())
            out = [tuple(x.strip() for x in (it.findtext("title") or "").split(" – ", 1)) for it in root.iter("item")]
            out = [t for t in out if len(t) == 2][:3]
            if out: return out
        except Exception as e: print("last.fm failed, using sample:", e, file=sys.stderr)
    return [("Mogwai", "Hunted by a Freak"), ("Idlewild", "A Film for the Future"), ("Biffy Clyro", "Many of Horror")]

# -------------------------------------------------------------------- buttons
def buttons():
    items = [("BLOG", "https://owain.codes"), ("LINKEDIN", "https://www.linkedin.com/in/owainwilliams/"),
             ("MASTODON", "https://umbracocommunity.social/@owaincodes"), ("COFFEE", "https://www.buymeacoffee.com/owaincodes")]
    md = []
    for i, (lab, url) in enumerate(items):
        w, h = 210, 44
        inner = f'<text x="{w/2}" y="29" font-size="18" text-anchor="middle" filter="url(#glow)">[ {lab} ]</text>'
        write(f"btn-{i}.svg", panel(w, h, inner, lab))
        md.append(f'<a href="{url}"><img src="assets/btn-{i}.svg" alt="{lab}" width="210"/></a>')
    return md

# --------------------------------------------------------------------- README
def inject(marker, lines):
    with open(README, encoding="utf-8") as f: s = f.read()
    a, b = f"<!-- {marker}:START -->", f"<!-- {marker}:END -->"
    new = a + "\n" + "\n".join(lines) + "\n" + b
    s = re.sub(re.escape(a) + ".*?" + re.escape(b), lambda _m: new, s, flags=re.S)
    with open(README, "w", encoding="utf-8") as f: f.write(s)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--user", default="OwainWilliams"); ap.add_argument("--sample", action="store_true")
    a = ap.parse_args()
    token = os.environ.get("GITHUB_TOKEN")
    sample = a.sample or not token
    if sample: days, totals = sample_days()
    else: days, totals = fetch_days(a.user, token)
    header()
    for n, l in (("bar-blog.svg", "INCOMING_MAIL.TXT"), ("bar-nuget.svg", "DIR C:\\PACKAGES /W"),
                 ("bar-contact.svg", "DIAL_UP --CONNECT"), ("bar-end.svg", "PRESS ANY KEY TO CONTINUE")):
        bar(n, l)
    contrib(days, totals, sample)
    audio(fetch_tracks(a.sample))
    inject("BLOG-POST-LIST", blog_rows(fetch_posts(a.sample)))
    pk, _ = pkg_rows(fetch_pkgs(a.sample)); inject("NUGET-LIST", pk)
    inject("BUTTONS", buttons())
    with open(os.path.join(ROOT, "demo", "data.json"), "w") as f:
        json.dump({"days": days, "totals": totals, "sample": sample}, f)
    print("done", "(sample data)" if sample else "(live data)")

if __name__ == "__main__": main()
