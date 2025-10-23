using DelEdAllotment.Data;
using DelEdAllotment.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DelEdAllotment.Pages
{
    public class UserProfileModel : PageModel
    {
        private readonly AppDbContext _context;

        public UserProfileModel(AppDbContext context)
        {
            _context = context;
        }

        public Registrations Registration { get; set; }
        public Centres Centre { get; set; }
        public string CityName { get; set; } = "N/A";
        public string CentreName { get; set; } = "Not Assigned";

        public void OnGet(int id)
        {
            // Fetch registration
            Registration = _context.Registration.FirstOrDefault(r => r.RegistrationNo == id);

            if (Registration != null && Registration.AssignedCentre.HasValue)
            {
                string assigned = Registration.AssignedCentre.Value.ToString();
                int cityCode = int.Parse(assigned.Substring(0, assigned.Length - 2));
                int centreCode = int.Parse(assigned.Substring(assigned.Length - 2, 2));

                // Fetch centre from database
                Centre = _context.Centre
                    .FirstOrDefault(c => c.CityCode == cityCode && c.CentreCode == centreCode && c.CentreTableSession == "2021-22");

                if (Centre != null)
                {
                    CityName = Centre.CityNameHindi;
                    CentreName = Centre.CentreNameHindi;
                }
                else
                {
                    CityName = "N/A";
                    CentreName = "N/A";
                }
            }

        }
    }
}
