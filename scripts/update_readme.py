import feedparser
import re
from datetime import datetime

FEED_URL = "https://owain.codes/rss"
README_PATH = "README.md"
MAX_POSTS = 5

START_MARKER = "<!-- BLOG-POST-LIST:START -->"
END_MARKER = "<!-- BLOG-POST-LIST:END -->"

def fetch_posts():
    feed = feedparser.parse(FEED_URL)
    posts = []
    for entry in feed.entries[:MAX_POSTS]:
        title = entry.title
        link = entry.link
        date = ""
        if hasattr(entry, "published_parsed") and entry.published_parsed:
            date = datetime(*entry.published_parsed[:3]).strftime("%b %d, %Y")
        posts.append(f"- [{title}]({link}){' — ' + date if date else ''}")
    return posts

def update_readme(posts):
    with open(README_PATH, "r") as f:
        content = f.read()

    new_section = START_MARKER + "\n" + "\n".join(posts) + "\n" + END_MARKER
    updated = re.sub(
        re.escape(START_MARKER) + ".*?" + re.escape(END_MARKER),
        new_section,
        content,
        flags=re.DOTALL
    )

    with open(README_PATH, "w") as f:
        f.write(updated)
    print(f"Updated README with {len(posts)} posts.")

if __name__ == "__main__":
    posts = fetch_posts()
    update_readme(posts)
