#!/usr/bin/env bash
# Release notes from the commit subjects. Only commits that change what the package ships
# (Source/ and thunderstore/) count, so docs and CI commits stay out.
#
#   scripts/changelog.sh notes <tag>   the commits of that release, as a Markdown list
#   scripts/changelog.sh full <tag>    CHANGELOG.md: every release up to <tag>, newest first
#
# Needs the full history and the tags (actions/checkout with fetch-depth: 0).
set -euo pipefail

shipped=(Source thunderstore)

# The release tag before <tag>, empty for the first release
previous_tag() {
    git describe --tags --abbrev=0 --match 'v*' "$1^" 2>/dev/null || true
}

notes() {
    local tag=$1 previous
    previous=$(previous_tag "$tag")
    local lines
    lines=$(git log --no-merges --format='- %s' "${previous:+$previous..}$tag" -- "${shipped[@]}")
    if [[ -n "$lines" ]]; then echo "$lines"; else echo "- No changes to the mod's files"; fi
}

full() {
    echo "# Changelog"
    local tag
    for tag in $(git tag --list 'v*' --sort=-v:refname --merged "$1"); do
        echo
        echo "## ${tag#v} ($(git log -1 --format=%as "$tag"))"
        echo
        notes "$tag"
    done
}

case "${1:-}" in
    notes) notes "$2" ;;
    full) full "$2" ;;
    *) echo "usage: $0 notes|full <tag>" >&2; exit 2 ;;
esac
