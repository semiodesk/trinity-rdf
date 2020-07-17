using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Semiodesk.Example;

namespace Asp.Net_Core_Example.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PatientController : ControllerBase
    {
        
        private readonly ILogger<PatientController> _logger;
        private readonly DataProvider _dataProvider;

        public PatientController(ILogger<PatientController> logger, DataProvider provider)
        {
            _logger = logger;
            _dataProvider = provider;
        }

        [HttpGet]
        public IEnumerable<Patient> Get()
        {
            return _dataProvider.ListPatients();
        }

        [HttpPost("update")]
        public void Update([FromBody]Patient patient)
        {
            _dataProvider.DefaultModel.UpdateResource(patient);
        }

        [HttpPost("add")]
        public void Add([FromBody]Patient patient)
        {
            _dataProvider.DefaultModel.AddResource(patient);
            patient.Commit();
        }

        [HttpDelete()]
        public void Remove([FromQuery]string uri)
        {
            _dataProvider.DefaultModel.DeleteResource(new Uri(uri));
        }
    }
}
