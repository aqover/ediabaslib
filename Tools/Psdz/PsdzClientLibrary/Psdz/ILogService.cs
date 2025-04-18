﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using BMW.Rheingold.Psdz.Model.Exceptions;

namespace BMW.Rheingold.Psdz
{
    public enum PsdzLoglevel
    {
        INFO = 1,
        FINE,
        DEBUG,
        TRACE,
        DEEP_TRACE
    }

    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ILogService
    {
        [OperationContract]
        [FaultContract(typeof(PsdzRuntimeException))]
        string ClosePsdzLog();

        [OperationContract]
        [FaultContract(typeof(PsdzRuntimeException))]
        void SetLogLevel(PsdzLoglevel psdzLoglevel);
    }
}
