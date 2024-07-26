using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AeroMech.API.Reports
{
	public class ServiceReportOrderInfoMiddle : IComponent

	{
		string _salesOrderNo;
		public ServiceReportOrderInfoMiddle(string salesOrderNo)
		{
			_salesOrderNo = salesOrderNo;
		}

		public void Compose(IContainer container)
		{
			var style = TextStyle.Default.FontSize(10).FontColor(Colors.Black);
			container.Column(column =>
			{
				column.Item().Row(row =>
				{
					row.RelativeItem().AlignCenter().Text("Sales Order No :").Style(style);
					row.RelativeItem().AlignCenter().Text(_salesOrderNo).Style(style);
				});				 
			});
		}
	}	 
}
