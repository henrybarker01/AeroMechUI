using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AeroMech.API.Reports
{
	public class QuoteOrderInfoRight : IComponent

	{
		string _jobNo;
		string _fsrNumber;
		public QuoteOrderInfoRight(string jobNo, string fsrNumber)
		{
			_jobNo = jobNo;
			_fsrNumber = fsrNumber;
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
                column.Item().Row(row =>
                {
                    row.ConstantColumn(50).Text("Report No :").AlignRight().Style(style);
                    row.ConstantColumn(50).Text(_fsrNumber).AlignRight().Style(style);
                });
            });
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
