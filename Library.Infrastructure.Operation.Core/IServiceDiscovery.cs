﻿using Library.Infrastructure.Operation.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Infrastructure.Operation.Core
{
    public interface IServiceDiscovery
    {
        void RegisterService(Service service);
    }
}
