﻿using OpenCqrs.Commands;

namespace OpenCqrs.Tests.Models.Commands;

public record SimpleCommand(string Name) : ICommand;
