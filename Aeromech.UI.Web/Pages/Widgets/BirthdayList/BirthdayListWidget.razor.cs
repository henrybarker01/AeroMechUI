using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.BirthdayList
{
    public partial class BirthdayListWidget
    {
        [Inject] ClientService ClientService { get; set; }
        [Inject] EmployeeService EmployeeService { get; set; }

        List<BirthdayList> birthdays { get; set; } = new List<BirthdayList>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                (await ClientService.GetClients()).ForEach(client =>
                {
                    if (client.ContactPersonBirthDate != null)
                        birthdays.Add(new BirthdayList()
                        {
                            Name = client.ContactPersonName,
                            Email = client.ContactPersonEmail,
                            PhoneNumber = client.ContactPersonNumber,
                            BirthDate = client.ContactPersonBirthDate?.ToString("dd/MM/yyyy"),

                        });
                });
                    (await EmployeeService.GetEmployees()).ForEach(employee =>
                    {
                        if (employee.BirthDate != null)
                            birthdays.Add(new BirthdayList()
                            {
                                Name = $"{employee.FirstName} {employee.LastName}",
                                Email = employee.Email,
                                PhoneNumber = employee.PhoneNumber,
                                BirthDate = employee.BirthDate?.ToString("dd/MM/yyyy")
                            });
                    });
                }
            await InvokeAsync(StateHasChanged);
        }
    }

    public class BirthdayList
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string BirthDate { get; set; }
    }
}