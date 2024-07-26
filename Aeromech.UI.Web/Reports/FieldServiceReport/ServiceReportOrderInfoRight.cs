using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AeroMech.API.Reports
{
	public class ServiceReportOrderInfoRight : IComponent

	{
		string _jobNo;
		public ServiceReportOrderInfoRight(string jobNo)
		{
			_jobNo = jobNo;
		}

		public void Compose(IContainer container)
		{
			var style = TextStyle.Default.FontSize(10).FontColor(Colors.Black);
			container.Column(column =>
			{
				column.Item().Row(row =>
				{
					row.ConstantColumn(50).Text("Job No :").AlignRight().Style(style);
					row.ConstantColumn(50).Text(_jobNo).AlignRight().Style(style);
				});
			});
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
