// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

namespace BRep.Extensions;

internal static class StaticResource
{
    public const string AboutMessage = """
                                       BUAA 2023 Autumn - Computer Aided Design And Manufacturing

                                       This is only a homework project, not a commercial product.
                                       
                                       Copyright © Tony's Studio 2023
                                       __________________________________________________
                                       Default model comes from:
                                       https://xoax.net/blog/rendering-transparent-3d-surfaces-in-wpf-with-c/
                                       """;

    public const string HelpMessage = """
                                      Interaction:
                                        Left click and drag to rotate the model
                                        Right click and drag to translate the model*
                                        Scroll the mouse wheel to zoom the model
                                        Click Reset View button to reset the view
                                        Click Change Color button to change the color of the model**
                                        Check Enable Muti-color to show each face of the model in different color***

                                      Menu:
                                        File | About: Show about information
                                        File | Help: Show help information
                                        File | Exit: Exit the program
                                        Load | Default Model: Load the default model
                                        Load | From File...: Load model from file
                                      __________________________________________________
                                      *: Translation will cause the rotation center to change
                                      **: Default model color cannot be changed
                                      ***: In some viewing angles, rendering errors may occur
                                      """;
}