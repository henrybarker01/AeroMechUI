using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AeroMech.API.Reports
{
    public class ServiceReportOrderInfoLeft : IComponent

    {
        OrderInfo _orderInfo;
        public ServiceReportOrderInfoLeft(OrderInfo orderInfo)
        {
            _orderInfo = orderInfo;
        }

        public void Compose(IContainer container)
        {
            var style = TextStyle.Default.FontSize(10).FontColor(Colors.Black);

            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.ConstantColumn(80).Text("Date :").Style(style);
                    row.RelativeItem().Text(_orderInfo.Date).Style(style);
                });
                column.Item().Row(row =>
                {
                    row.ConstantColumn(80).Text("Client :").Style(style);
                    row.RelativeItem().Text(_orderInfo.Client).Style(style);
                });
                column.Item().Row(row =>
                {
                    row.ConstantColumn(80).Text("Machine Type :").Style(style);
                    row.RelativeItem().Text(_orderInfo.MachineType).Style(style);
                });
                column.Item().Row(row =>
                {
                    row.ConstantColumn(80).Text("Serial Number :").Style(style);
                    row.RelativeItem().Text(_orderInfo.SerialNumber).Style(style);
                });
                column.Item().Row(row =>
                {
                    row.ConstantColumn(80).Text("Engine Hours :").Style(style);
                    row.RelativeItem().Text(_orderInfo.EngineHours).Style(style);
                });
                column.Item().Row(row =>
                {
                    row.ConstantColumn(80).Text("Instructions :").Style(style);
                    row.RelativeItem().Text(_orderInfo.Instructions).Style(style);
                });
                column.Spacing(10);

            });
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
