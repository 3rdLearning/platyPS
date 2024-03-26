# PlatyPS

[Original PlatyPS project README](README-PROJECT.md)

PlatyPS is a PowerShell module that is used to update and maintain module help documentation and it is used by PLS 3rd Learning as part of its PowerShell module template.

This is an internal fork of PlatyPS that resolves an issue with the generation of help documentation that doesn't take into account new common parameters introduced in PowerShell version 7.4.  This causes extra parameters to be included in the help documentation that shouldn't be there.  See [platyPS Issue `#595](https://github.com/PowerShell/platyPS/issues/595) for more information.  There is a pending pull request that resolves this but it doesn't look like it will be merged or fixed in version 1.x, and version 2.x remains unreleased.

## Quick start

* Install internal fork of the platyPS module from the PLS3rdLearning repository

```powershell
Install-Module -Name platyPS -Scope CurrentUser -Repository PLS3rdLearning
Import-Module platyPS
```
See the [Original PlatyPS project README](README-PROJECT.md) for additional usage information.

# Making a new release
See [contributing guidelines](CONTRIBUTING.md) for more information on development requirements.

1. Make sure platyPS help itself (content in .\docs folder) is up to date. 
   `Update-MarkdownHelp -Path .\docs` should result in no changes unless a new parameter is added.
1. Change the version in platyps.psd1.  The original module doesn't use it, but the internal fork uses it for publishing
1. Publish the module to the Gallery: `.\build.ps1 -Configuration Release -Publish:$true`.
1. Check the PLS3rdLearning repostiory to verify it is updated.

Congratulations! You just made a release.