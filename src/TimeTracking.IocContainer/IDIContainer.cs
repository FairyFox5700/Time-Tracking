﻿using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace TimeTracking.IocContainer
{
    public interface IDIContainer
    {
        IServiceCollection Services { get; }
        void AddService(ServiceDescriptor descriptor);
    }
}