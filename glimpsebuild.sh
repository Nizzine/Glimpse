#!/bin/bash

# Builds glimpse for release.
# Commented because I almost always immediately forget how to read and write bash scripts.
# I swear this stuff is black magic... And then I look at winetricks and I just think HOW?? and WHY??

if [ $# -lt 3 ]; then
  echo "Usage: glimpsebuild <out_name> <runtime> <version> [--nozip] [--aot]"
  exit 2
fi

NOZIP=false
AOT=false

for arg in "$@"; do
  if [ $arg == "--nozip" ]; then
    NOZIP=true
  fi
  if [ $arg == "--aot" ]; then
    AOT=true
    echo "AOT compilation not yet implemented!"
    exit 0
  fi
done

OUT_NAME="$1"
BUILD_DIR_NAME="${OUT_NAME}"
BUILD_DIR="$(pwd)/$BUILD_DIR_NAME"
RUNTIME=$2
VERSION=$3
PLUGINS_DIR="$BUILD_DIR/Plugins"
PLUGINS_DIR_TEMP="$BUILD_DIR/PluginsTEMP"

# Ensure output directory is reset.
rm -rf $BUILD_DIR || exit 1
mkdir -p $BUILD_DIR || exit 1

# Publish main glimpse program.
dotnet publish -c Release -r $RUNTIME -o $BUILD_DIR -p:Version="$VERSION" --self-contained src/Glimpse || exit 1

# As glimpse uses MixrSharp as a project reference rather than a nuget, we must delete the library file that doesn't
# match the output runtime.
pushd $BUILD_DIR
rm Silk.NET.SDL.dll

# TODO: There MUST be a better way to do this.
if [[ $RUNTIME == "win"* ]]; then
  rm libmixr.so
  rm libempress.so
  rm libmixr.dylib
  rm SDL2.dll
elif [[ $RUNTIME == "osx"* ]]; then
  rm libmixr.so
  rm libempress.so
  rm mixr.dll
  rm libSDL2-2.0.dylib
elif [[ $RUNTIME == "linux"* ]]; then
  rm mixr.dll
  rm libmixr.dylib
  rm libSDL2-2.0.so
fi
popd

# Build and copy the dependencies of all plugins.
for dir in Plugins/*; do
  pushd $dir || exit 1
  # If there is no Dependencies.txt, then it's not a plugin!
  if [ -f "Dependencies.txt" ]; then
    dir_name="${dir##*/}"
    plugin_temp_dir="$PLUGINS_DIR_TEMP/$dir_name"
    plugin_BUILD_DIR="$PLUGINS_DIR/$dir_name"
    
    mkdir -p $plugin_temp_dir
    mkdir -p $plugin_BUILD_DIR
    
    # Publish to a temporary directory first.
    dotnet publish -c Release -r $RUNTIME -o $plugin_temp_dir -p:Version="$VERSION" . || exit 1
    
    # Read the list of dependencies. Native dependencies are marked with a *, which get special treatment.
    # Only the native deps that fit the output runtime are copied.
    # TODO: This doesn't read the last line of the file... A blank line at the end of the file has to be inserted.
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
      cp "$plugin_temp_dir/$dep" "$plugin_BUILD_DIR" || exit 1
    done <"Dependencies.txt"
  fi
  popd || exit 1
done

rm -rf $PLUGINS_DIR_TEMP || exit 1

# If --nozip, don't compress the output.
if $NOZIP; then
  exit 0
fi

if [[ $RUNTIME == "win"* ]]; then
  curl -LO https://aka.ms/vc14/vc_redist.x64.exe || exit 1
  mv vc_redist.x64.exe "$BUILD_DIR_NAME/" || exit 1
  zip -r "$OUT_NAME-$RUNTIME.zip" "$BUILD_DIR_NAME/" || exit 1
  makensis -DVERSION="$VERSION" -DPUBLISHDIR="$BUILD_DIR" ./packaging/windows/glimpse.nsi || exit 1
elif [[ $RUNTIME == "osx"* ]]; then
  # Create a macOS build and mvoe the output here (as it outputs into packaging/macos/Glimpse.app)
  # Then copy the contents of the build directory into the MacOS directory
  # TODO: Find out why it produces a package with a cross through it??
  ./packaging/macos/build-macos.sh $VERSION || exit 1
  mv "packaging/macos/Glimpse.app" . || exit 1
  cp -a "$BUILD_DIR_NAME/." "Glimpse.app/Contents/MacOS" || exit 1
  zip -r "$OUT_NAME-$RUNTIME.zip" "Glimpse.app" || exit 1
elif [[ $RUNTIME == "linux"* ]]; then
  mkdir -p "$BUILD_DIR/bin"
  mv $BUILD_DIR/* "$BUILD_DIR/bin"
  cp -r ./packaging/linux/installer/. $BUILD_DIR/ || exit 1
  tar -czvf "$OUT_NAME-$RUNTIME.tar.gz" "$BUILD_DIR_NAME/" || exit 1
fi

rm -rf $BUILD_DIR || exit 1
