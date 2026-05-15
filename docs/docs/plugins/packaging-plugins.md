# Building and Packaging Plugins
So... your plugin is ready to be packaged up, ready to be shipped.
Or, you just want an easy and reliable way to test it.

Fortunately, the Glimpse SDK makes this easy.

Plugins are shipped in the Glimpse Plugin format. These files have the extension `.gplg`. Fundamentally, a gplg file is
a zip archive, containing a modified `Plugin.json` file, and the actual plugin assemblies (and any native dependencies).
When opening these files with Glimpse, it will extract the archive, ensure its contents, and then copy the files into
the correct plugin folders.

These plugin files are simply an easy way to ship plugins containing all the necessary binaries to be installed on any
supported platform. The user only has to download a single file, drag it into Glimpse, and it handles the rest.
Plugins *can* be manually installed, and, during development, you will have likely done this, however it is not
practical to expect a user to manually install plugins.

## package-plugin
Bundled with the Glimpse SDK is a program called `package-plugin`. This is a Command-Line (CLI) program that takes your
plugin, builds it, and spits out a `.gplg` file.

### Using the Tool
The most basic usage is `package-plugin ./path-to-a-plugin-folder`, where the plugin folder is the **source code** for
the plugin. Much like when loading plugins manually, there **MUST** be a `Plugin.json` file present. The packager does
not implicitly create one for you.

The path to the plugin folder should be the folder containing the `.csproj` file for your plugin. Typically, the plugin
folder, and the `.csproj` file will have the same name. If it does not, you can specify the name of the project file
by providing `-p <project name>` to the command (you **do not** need to include the file extension).

### Native dependencies

If everything succeeds, the tool will output a `.gplg` file in your current working directory. This file contains a
modified `Plugin.json` file, where the native dependencies (if there are any) are specified in a `NativeDeps` property.

Where possible, the detection and copying of native dependencies is done automatically, and likewise, when installing a
plugin, Glimpse will automatically choose the correct set of dependencies to install for the current platform.

If your plugin contains binaries that are only available for some platforms (for example, your plugin uses a binary that
only is available for Windows), then only users on the supported platforms will be able to install your plugin.
Non-supported platforms will be shown an error. If your plugin is available for download on the website, this will also
be shown there.

Because of this, it is highly recommended that you avoid native dependencies wherever possible, and stick to purely C#
code. This will ensure your plugin can run on every supported platform, and does not need to be recompiled. If you do
need to use native dependencies, it is recommended that you support at least Windows (x64) and Linux (x64), which are
the two primary platforms. You should also try and support macOS (Apple Silicon, arm64) if possible as well. There is
no need to support Intel macs, as Glimpse does not support Intel macs.

#### Copying Native Dependencies Manually
If your plugin includes custom native dependencies, for example, through directly adding them to your plugin, or via a
submodule, there's a good chance those dependencies are not packed in a way that `package-plugin` can detect. If this is
the case, you can manually specify in the `Plugin.json` file which dependencies to copy, and for each platform:

```json
{
  /* ... */
  "NativeDeps": {
    "win-x64": [
      "mynativedep.dll"
    ],
    "linux-x64": [
      "libmynativedep.so"
    ],
    "osx-arm64": [
      "libmynativedep.dylib"
    ]
  }
}
```

When specifying dependencies manually, you must use the appropriate
[Runtime Identifiers](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids) for your target platforms.

Much like before, if the user tries to install your plugin on a platform that your plugin does not support, the user
will not be able to install your plugin.