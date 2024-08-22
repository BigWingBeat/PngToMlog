# PngToMlog
Converts a png image to mlog commands. Uses a lightweight vectorization method written by Vortetty to optimize the output. Runs cross-platform on the command line. Requires at least .NET 8.

## Usage
```
PngToMlog version 3, BigWingBeat fork

Program to convert images into mlog commands

Usage: pngtomlog <flags> <image path>

Available flags:

        -h --help
                Display this help message

        -v --version
                Display program version

        -s --small
                Output for Small Logic Display (80x80 resolution)

        -l --large
                Output for Large Logic Display (176x176 resolution)

        -i -ipp
                Set instructions-per-processor. Defaults to 990 if unset

        -m --mode
                Set scaling mode for the image: Possible options are:

                scale
                        Scales the image on each axis to exactly fit the display resolution
                        Will result in visible squashing / stretching for images that are
                        significantly larger / smaller than the display on each axis

                letterbox
                        Scale the entire image uniformly until it fits within the display resolution
                        Will result in blank bars above & below the image for landscape images,
                        or either side of the image for portrait images.
                        Images with an extreme aspect ratio will be very thin and likely hard to make out

                crop
                        Do not scale the image in any way.
                        Images larger than the display will be cut off at the sides,
                        and images smaller than the display will result in blank bars

                If no mode is specified, it defaults to 'scale'
```

After the program finishes running, there will be a set of files named `0.txt`, `1.txt` etc. in the directory you ran it in. Each of these files corresponds to a logic processor. To import it into mindustry, place the logic processor in-game, open the corresponding txt file in a text editor, copy the entire contents of the file, and then paste it into the game by clicking 'edit' -> 'import from clipboard' in the logic processor UI.
