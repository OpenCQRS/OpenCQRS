﻿using OpenCqrs.Commands;

namespace OpenCqrs.Tests.Models.Commands;

public record FirstCommandInSequence(string Name) : ICommand<string>;
