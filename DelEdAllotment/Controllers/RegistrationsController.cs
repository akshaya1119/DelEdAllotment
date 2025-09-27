using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DelEdAllotment.Data;
using DelEdAllotment.Models;

namespace DelEdAllotment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RegistrationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Registrations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Registrations>>> GetRegistration()
        {
            return await _context.Registration.ToListAsync();
        }

        // GET: api/Registrations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Registrations>> GetRegistrations(int id)
        {
            var registrations = await _context.Registration.FindAsync(id);

            if (registrations == null)
            {
                return NotFound();
            }

            return registrations;
        }

        // PUT: api/Registrations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRegistrations(int id, Registrations registrations)
        {
            if (id != registrations.Id)
            {
                return BadRequest();
            }

            _context.Entry(registrations).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RegistrationsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Registrations
        [HttpPost]
        public async Task<ActionResult<Registrations>> PostRegistrations(Registrations registrations)
        {
            _context.Registration.Add(registrations);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRegistrations", new { id = registrations.Id }, registrations);
        }

        // DELETE: api/Registrations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegistrations(int id)
        {
            var registrations = await _context.Registration.FindAsync(id);
            if (registrations == null)
            {
                return NotFound();
            }

            _context.Registration.Remove(registrations);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RegistrationsExists(int id)
        {
            return _context.Registration.Any(e => e.Id == id);
        }

        // 🔥 NEW API: Allocate Centres
        [HttpPost("allocate-centres")]
        public async Task<IActionResult> AllocateCentres()
        {
            try
            {
                // Load registrations sorted
                var registrations = await _context.Registration
                    .OrderBy(r => r.Name)
                    .ThenBy(r => r.DOB)
                    .ThenBy(r => r.RegistrationNo)
                    .ToListAsync();

                var centres = await _context.Centre.ToListAsync();

                // Track current fills (not touching DB directly)
                var centreCounts = centres.ToDictionary(c => c.Id, c => 0);

                foreach (var reg in registrations)
                {
                    var preferredCity = reg.PreferredCityCode;

                    // Get all centres in the preferred city, except nodal
                    var cityCentres = centres
                        .Where(c => c.CityCode == preferredCity && c.CentreCode != 1)
                        .OrderBy(c => c.CentreCode)
                        .ToList();

                    var assigned = false;

                    // Try normal centres
                    foreach (var centre in cityCentres)
                    {
                        if (centreCounts[centre.Id] < centre.Capacity)
                        {
                            reg.AssignedCentre = (centre.CityCode * 100) + centre.CentreCode; ; // 👈 Save CityCode
                            centreCounts[centre.Id]++;
                            assigned = true;
                            break;
                        }
                    }

                    // If not possible, assign nodal
                    if (!assigned)
                    {
                        var nodalCentre = centres
                            .FirstOrDefault(c => c.CityCode == preferredCity && c.CentreCode == 1);

                        if (nodalCentre != null)
                        {
                            reg.AssignedCentre = (nodalCentre.CityCode * 100) + nodalCentre.CentreCode; // 👈 Still CityCode
                            centreCounts[nodalCentre.Id]++;

                            // Increase nodal capacity if overflow
                            if (centreCounts[nodalCentre.Id] > nodalCentre.Capacity)
                            {
                                nodalCentre.IncreasedCapacity =
                                    centreCounts[nodalCentre.Id] - nodalCentre.Capacity;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Centres allocated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error during centre allocation", details = ex.Message });
            }
        }

        // 🔥 NEW API: Allocate Roll Numbers
        [HttpPost("allocate-rollnumbers")]
        public async Task<IActionResult> AllocateRollNumbers()
        {
            try
            {
                // Load registrations sorted by Name then DOB
                var registrations = await _context.Registration
                    .OrderBy(r => r.Name)
                    .ThenBy(r => r.DOB)
                    .ThenBy(r => r.RegistrationNo)
                    .ToListAsync();

                // Group by AssignedCentre
                var groupedByCentre = registrations
                    .GroupBy(r => r.AssignedCentre)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var group in groupedByCentre)
                {
                    var assignedCentre = group.Key; // e.g., 1, 102, 1306
                    int cityCode = assignedCentre.Value / 100;  // 1 for 102 or 13 for 1306
                    int centreCode = assignedCentre.Value % 100; // 2 for 102 or 6 for 1306

                    int serial = 1;
                    foreach (var reg in group.Value)
                    {
                        string yearPart = "23";
                        //string yearPart = DateTime.Now.Year.ToString().Substring(2, 2); // "25"
                        string cityPart = cityCode.ToString("D2"); // 2 digits
                        string centrePart = centreCode.ToString("D2"); // 2 digits
                        string serialPart = serial.ToString("D3"); // 3 digits

                        reg.RollNumber = int.Parse($"{yearPart}{cityPart}{centrePart}{serialPart}");
                        serial++;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Roll numbers allocated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error during roll number allocation", details = ex.Message });
            }
        }



    }
}
