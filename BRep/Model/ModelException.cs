// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System;

namespace BRep.Model;

internal class ModelException : Exception
{
    public ModelException(string message) : base(message)
    {
    }
}