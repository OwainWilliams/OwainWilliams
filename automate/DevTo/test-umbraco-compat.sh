#!/usr/bin/env bash
# Checks the package works on every Umbraco major it claims to support.
#
#   1. Build and test against Umbraco 17 (what the package is compiled against and ships).
#   2. Build and test against Umbraco 18 (source compatibility).
#   3. Run the Umbraco 18 test build with the DLL compiled against 17 swapped in. This is
#      what an Umbraco 18 site actually runs, and catches members that moved between
#      majors (MissingMethodException), which step 2 can't see.
#   4. Check every Umbraco type/member the 17 build references exists in the 18 assemblies,
#      covering the code paths the tests in step 3 don't reach.
#
# Usage: ./test-umbraco-compat.sh   (from anywhere; needs the .NET 10 SDK)
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
package="$here/Umbraco.Community.Automate.DevTo"
tests="$here/Umbraco.Community.Automate.DevTo.Tests"
shipped="$(mktemp -d)"
trap 'rm -rf "$shipped"' EXIT

echo "::group::Umbraco 17"
dotnet test "$tests" -c Release -p:UmbracoMajor=17
cp "$package/bin/Release/net10.0/Umbraco.Community.Automate.DevTo.dll" "$shipped/"
echo "::endgroup::"

echo "::group::Umbraco 18"
dotnet test "$tests" -c Release -p:UmbracoMajor=18
echo "::endgroup::"

echo "::group::Umbraco 17 build running on Umbraco 18"
cp "$shipped/Umbraco.Community.Automate.DevTo.dll" "$tests/bin/Release/net10.0/"
dotnet test "$tests" -c Release -p:UmbracoMajor=18 --no-build
echo "::endgroup::"

echo "::group::Umbraco references in the 17 build resolve on Umbraco 18"
# ASP.NET Core's shared framework, which Umbraco's assemblies reference.
aspnet="$(dotnet --list-runtimes | awk '$1 == "Microsoft.AspNetCore.App" && $2 ~ /^10\./ { gsub(/[][]/, "", $3); dir = $3 "/" $2 } END { print dir }')"
dotnet run --project "$here/tools/ReferenceCheck" -c Release -- \
  "$shipped/Umbraco.Community.Automate.DevTo.dll" "$tests/bin/Release/net10.0" "$aspnet"
echo "::endgroup::"

# Leave the working tree built for the default (17) again.
dotnet restore "$tests" -p:UmbracoMajor=17 > /dev/null
