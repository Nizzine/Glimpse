#!/bin/bash

if [ $# -ne 3 ]; then
  echo "Usage: glimpsebuild <out-dir> <runtime> <version>"
  exit 2
fi

OUT_DIR="$(pwd)/$1"
RUNTIME=$2
VERSION=$3
PLUGINS_DIR="$OUT_DIR/Plugins"
PLUGINS_DIR_TEMP="$OUT_DIR/PluginsTEMP"

# Ensure output directory is reset.
rm -rf $OUT_DIR || exit 1
mkdir -p $OUT_DIR || exit 1

# Publish main glimpse program.
dotnet publish -c Release -r $RUNTIME -o $OUT_DIR -p:Version="$VERSION" src/Glimpse || exit 1

# As glimpse uses MixrSharp as a project reference rather than a nuget, we must delete the library file that doesn't
# match the output runtime.
pushd $OUT_DIR
if [[ $RUNTIME == "win"* ]]; then
  rm libmixr.so || exit 1
elif [[ $RUNTIME == "linux"* ]]; then
  rm mixr.dll || exit 1
fi
popd

# Build and copy the dependencies of all plugins.
for dir in Plugins/*; do
  pushd $dir || exit 1
  # If there is no Dependencies.txt, then it's not a plugin!
  if [ -f "Dependencies.txt" ]; then
    dir_name="${dir##*/}"
    plugin_temp_dir="$PLUGINS_DIR_TEMP/$dir_name"
    plugin_out_dir="$PLUGINS_DIR/$dir_name"
    
    mkdir -p $plugin_temp_dir
    mkdir -p $plugin_out_dir
    
    # Publish to a temporary directory first.
    dotnet publish -c Release -r $RUNTIME -o $plugin_temp_dir -p:Version="$VERSION" . || exit 1
    
    # Read the list of dependencies. Native dependencies are marked with a *, which get special treatment.
    # Only the native deps that fit the output runtime are copied.
    while read dep; do
      if [[ $dep == *"*" ]]; then
        dep=${dep%%"*"} 
        if [[ $dep == *.dll ]] && [[ $RUNTIME != "win"* ]]; then
          continue
        elif [[ $dep == *.so* ]] && [[ $RUNTIME != "linux"* ]]; then
          continue
        fi
      fi
      # Copy the dependency to the main plugins directory.
      cp "$plugin_temp_dir/$dep" "$plugin_out_dir" || exit 1
    done <"Dependencies.txt"
  fi
  popd || exit 1
done

rm -rf $PLUGINS_DIR_TEMP || exit 1