using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RepNaviOpr01.Properties;
using RepNaviOpr01.TempClasses;
using Transnavi.DataEntity.DbInterop;
using Transnavi.DataEntity.DbInterop.Selector.Comparer;
using Transnavi.ReportEntity;
using Transnavi.ReportEntity.FormBuilding;
using Transnavi.ReportEntity.Forms.FormTime;

namespace RepNaviOpr01
{
    public class CurBusStateReportItemWithParameter : ReportItemWithParameter<ITimeParameter>
    {
        private readonly QueryResult<ReportRow> m_queryResult;

        public CurBusStateReportItemWithParameter(IPrimaryParameterRequest inputParameter, ShellBuildingRequest<ITimeParameter> shellRequestRequest)
            : base(inputParameter, shellRequestRequest)
        {
            m_queryResult = new QueryResult<ReportRow>(this);
        }

        protected override void ExecDataBy(IDbAccessor dbAccessor)
        {
            foreach (var parkId in InputParameter.GetParkIds())
            {
                var query = new QueryCommand(InputParameter, parkId, RequestParameters);
                m_queryResult.ExecData(dbAccessor, query);  
            }
        }

        protected override ReportVersionBase GetReportVersion()
        {
            var parameter = RequestParameters ?? GetDefaultParameter();
            return new ReportVersion(m_queryResult.Result, InputParameter, parameter);
        }

        private class ReportVersion : ReportVersionBase
        {
            private readonly IEnumerable<ReportRow> m_dataSource;
            private readonly IPrimaryParameterRequest m_inputParameter;
            private readonly ITimeParameter m_parameter;

            public ReportVersion(IEnumerable<ReportRow> dataSource, IPrimaryParameterRequest inputParameter, ITimeParameter parameter)
            {
                m_dataSource = dataSource;
                m_inputParameter = inputParameter;
                m_parameter = parameter;
            }

#if DEBUG
            protected override IEnumerable<T> CreateReportCoreDebug<T>(IReportBuilder<T> reportBuilder)
            {
                var reportCreator = new ReportCreatorFromFile(Resources.reportPath);

                var rep = reportCreator.Create(c_editVersion, reportBuilder);
                RegisterData(rep, c_editVersion, m_dataSource);

                return new[] { rep };
            }
#else
            protected override IEnumerable<T> CreateReportCoreRelease<T>(ReportCreatorFromStream reportCreator, IReportBuilder<T> reportBuilder)
            {
                var reprtList = new List<T>();

                var groupByPark = m_dataSource.GroupBy(d => d.Park, d => d, new ParkComparer()).ToList();
                foreach (var park in groupByPark)
                {
                    string caption = park.Key.Title;
                    var rep = reportCreator.Create(caption, reportBuilder);

                    var parkOrderMarsh = park.OrderBy(p => p.Marsh, new RouteSpecialComparer()).ToList();

                    RegisterData(rep, caption, parkOrderMarsh);
                    reprtList.Add(rep);
                }

                return reprtList;
            }

            protected override ReportCreatorFromStream GetReportCreator()
            {
                return new ReportCreatorFromStream(Resources.Отчет_о_состоянии_процесса_перевозок_по_парку);
            }
#endif
            private void RegisterData(IReport report, string header, IEnumerable source)
            {
                report.SetParameterValue("@Header", header);
                report.SetParameterValue("@TimeParam", m_parameter.CurrentTime);
                report.SetParameterValue("@DateParam", m_inputParameter.DateRequest);
                report.RegisterData(source, "name");
            }
        }
    }
}
