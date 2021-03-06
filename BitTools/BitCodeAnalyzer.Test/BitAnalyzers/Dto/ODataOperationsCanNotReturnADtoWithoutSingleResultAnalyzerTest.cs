﻿using BitCodeAnalyzer.BitAnalyzers.Dto;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace BitCodeAnalyzer.Test.BitAnalyzers.Dto
{
    [TestClass]
    public class ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzerTest : DiagnosticVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public async Task FindODataOperationWhichReturnADtoWithoutSingleResult()
        {
            const string source = @"
using System.Linq;
using System.Collections.Generic;

public interface IDto { }

public class CustomerDto : IDto { }

public class SingleResult<TDto>
{
}

public class CustomersController : DtoController<CustomerDto>
{
    [ActionAttribute]
    public async Task<CustomerDto> GetCustomer1() { return null; }
    [ActionAttribute]
    public async Task<SingleResult<CustomerDto>> GetCustomer2() { return null; }
    [ActionAttribute]
    public async Task<CustomerDto[]> GetCustomer3() { return null; }
    [ActionAttribute]
    public async Task<IQueryable<CustomerDto>> GetCustomer4() { return null; }
    [ActionAttribute]
    public async Task<IEnumerable<CustomerDto>> GetCustomer5() { return null; }
    [ActionAttribute]
    public CustomerDto GetCustomer6() { return null; }
    [ActionAttribute]
    public SingleResult<CustomerDto> GetCustomer7() { return null; }
    [ActionAttribute]
    public CustomerDto[] GetCustomer8() { return null; }
    [ActionAttribute]
    public IQueryable<CustomerDto> GetCustomer9() { return null; }
    [ActionAttribute]
    public IEnumerable<CustomerDto> GetCustomer10() { return null; }
}

";

            DiagnosticResult[] errors = new DiagnosticResult[] {
                 new DiagnosticResult {
                        Id = nameof(ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzer),
                        Message = string.Format(ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzer.Message, "CustomerDto"),
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation(15, 5) }
                 },
                    new DiagnosticResult {
                        Id = nameof(ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzer),
                        Message = string.Format(ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzer.Message, "CustomerDto"),
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation(25, 5) }
                 }
            };


            await VerifyCSharpDiagnostic(source, errors);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzer();
        }
    }
}
