import re
import urllib.request
import json

README_PATH = "README.md"
NUGET_OWNER = "owain"  # Your NuGet username

START_MARKER = "<!-- NUGET-LIST:START -->"
END_MARKER = "<!-- NUGET-LIST:END -->"

def fetch_nuget_packages():
    url = f"https://azuresearch-usnc.nuget.org/query?q=owner:scottishcoder&take=50"
    with urllib.request.urlopen(url) as response:
        data = json.loads(response.read())

    rows = []
    for pkg in data["data"]:
        name = pkg["id"]
        pkg_url = f"https://www.nuget.org/packages/{name}"
        version_badge = f"![NuGet](https://img.shields.io/nuget/v/{name})"
        download_badge = f"![Downloads](https://img.shields.io/nuget/dt/{name})"
        rows.append(f"| [{name}]({pkg_url}) | {version_badge} | {download_badge} |")

    header = "| Package | Version | Downloads |\n|---------|---------|-----------|"
    return [header] + rows

def update_readme(packages):
    with open(README_PATH, "r") as f:
        content = f.read()

    new_section = START_MARKER + "\n" + "\n".join(packages) + "\n" + END_MARKER
    updated = re.sub(
        re.escape(START_MARKER) + ".*?" + re.escape(END_MARKER),
        new_section,
        content,
        flags=re.DOTALL
    )

    with open(README_PATH, "w") as f:
        f.write(updated)
    print(f"Updated README with {len(packages) - 1} packages.")

if __name__ == "__main__":
    packages = fetch_nuget_packages()
    update_readme(packages)
```

Also **revert `update_readme.py`** back to only handling blog posts — remove the NuGet functions you added in the previous step, so each script has a single responsibility.

---

Your repo structure should now look like this:
```
.github/
  workflows/
    blog-posts.yml       ← runs at 6am UTC
    nuget-packages.yml   ← runs at 7am UTC
scripts/
  update_readme.py       ← blog posts only
  update_nuget.py        ← nuget packages only
README.md
