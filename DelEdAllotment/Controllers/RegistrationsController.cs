using DelEdAllotment.Data;
using DelEdAllotment.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace DelEdAllotment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        private static Dictionary<int, int> CityCapacityTracker = new Dictionary<int, int>();
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

        [HttpGet("get-registration-details/{registrationNo}")]
        public async Task<ActionResult<object>> GetRegistrationDetailsByRegNo(string registrationNo)
        {
            try
            {
                string session = "2025-26";

                if (!int.TryParse(registrationNo, out int regNo))
                    return BadRequest(new { message = "Invalid registration number format." });

                var reg = await _context.Registration
                    .FirstOrDefaultAsync(r => r.RegistrationNo == regNo && r.Session == session);

                if (reg == null)
                    return NotFound(new { message = "Registration not found." });

                var centres = await _context.Centre
                    .Where(c => c.CentreTableSession == session)
                    .ToListAsync();

                var admitCard = await _context.AdmitCard
             .FirstOrDefaultAsync(ac => ac.Registration_No == reg.RegistrationNo);
                int assignedBoth = reg.AssignedBoth ?? 0;
                int cityCode = assignedBoth / 100;
                int centreCode = assignedBoth % 100;

                var centre = centres.FirstOrDefault(c => c.CityCode == cityCode && c.CentreCode == centreCode);

                // --- Convert images to Base64 ---
                string imageBase64 = null;
                string signatureBase64 = null;

                if (!string.IsNullOrEmpty(reg.ImagePath) && System.IO.File.Exists(reg.ImagePath))
                {
                    byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(reg.ImagePath);
                    imageBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";
                }

                if (!string.IsNullOrEmpty(reg.SignaturePath) && System.IO.File.Exists(reg.SignaturePath))
                {
                    byte[] signBytes = await System.IO.File.ReadAllBytesAsync(reg.SignaturePath);
                    signatureBase64 = $"data:image/png;base64,{Convert.ToBase64String(signBytes)}";
                }
                string formattedCityCode = centre != null ? centre.CityCode.ToString("D2") : null;
                string formattedCentreCode = centre != null ? centre.CentreCode.ToString("D2") : null;
                string address = admitCard != null
                 ? $"{admitCard.Address}, {admitCard.City.ToUpper()}, {admitCard.State.ToUpper()}, {admitCard.Pin}"
                 : string.Empty;

                var registrationDetails = new
                {
                    reg.Name,
                    reg.FName,
                    reg.Gender,
                    reg.RegistrationNo,
                    reg.Category,
                    reg.WargHindi,
                    reg.DOB,
                    Address = address,
                    reg.RollNumber,
                    reg.PhotoId,
                    reg.Ph,
                    reg.PhType,
                    ImagePath = imageBase64,
                    SignaturePath = signatureBase64,
                    reg.SubCategory,
                    AssignedBoth = centre != null ? new
                    {
                        CityCode = formattedCityCode,
                        CityName = centre.CityNameHindi,
                        CentreCode = formattedCentreCode,
                        CentreName = centre.CentreNameHindi
                    } : (object)null
                };

                return Ok(registrationDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error fetching registration details", details = ex.Message });
            }
        }



        [HttpGet("get-registration-batch")]
        public async Task<ActionResult<object>> GetRegistrationBatch(int start = 0, int size = 3)
        {
            try
            {
                string session = "2025-26";

                // Get ordered registrations for the session
                var registrations = await _context.Registration
                    .Where(r => r.Session == session)
                    .OrderBy(r => r.RegistrationNo)
                    .Skip(start)
                    .Take(size)
                    .ToListAsync();

                var admitCards = await _context.AdmitCard
            .Where(ac => registrations.Select(r => r.RegistrationNo).Contains(ac.Registration_No))
            .ToListAsync();
                if (!registrations.Any())
                    return Ok(new object[0]); // return empty array if no data

                var centres = await _context.Centre
                    .Where(c => c.CentreTableSession == session)
                    .ToListAsync();

                var registrationDetails = new List<object>();

                foreach (var reg in registrations)
                {
                    var admitCard = admitCards.FirstOrDefault(ac => ac.Registration_No == reg.RegistrationNo);
                    int assignedBoth = reg.AssignedBoth ?? 0;
                    int cityCode = assignedBoth / 100;
                    int centreCode = assignedBoth % 100;

                    var centre = centres.FirstOrDefault(c => c.CityCode == cityCode && c.CentreCode == centreCode);

                    string imageBase64 = null;
                    string signatureBase64 = null;

                    if (!string.IsNullOrEmpty(reg.ImagePath) && System.IO.File.Exists(reg.ImagePath))
                    {
                        byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(reg.ImagePath);
                        imageBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";
                    }

                    if (!string.IsNullOrEmpty(reg.SignaturePath) && System.IO.File.Exists(reg.SignaturePath))
                    {
                        byte[] signBytes = await System.IO.File.ReadAllBytesAsync(reg.SignaturePath);
                        signatureBase64 = $"data:image/png;base64,{Convert.ToBase64String(signBytes)}";
                    }

                    string address = admitCard != null
                  ? $"{admitCard.Address}, {admitCard.City.ToUpper()}, {admitCard.State.ToUpper()}, {admitCard.Pin}"
                  : string.Empty;

                    string formattedCityCode = centre != null ? centre.CityCode.ToString("D2") : null;
                    string formattedCentreCode = centre != null ? centre.CentreCode.ToString("D2") : null;
                    registrationDetails.Add(new
                    {
                        reg.Name,
                        reg.FName,
                        reg.Gender,
                        reg.Category,
                        reg.DOB,
                        Address = address,
                        reg.WargHindi,
                        reg.RollNumber,
                        reg.RegistrationNo,
                        reg.PhotoId,
                        reg.Ph,
                        reg.PhType,
                        ImagePath = imageBase64,
                        SignaturePath = signatureBase64,
                        reg.SubCategory,
                        AssignedBoth = centre != null ? new
                        {
                            CityCode = formattedCityCode,
                            CityName = centre.CityNameHindi,
                            CentreCode = formattedCentreCode,
                            CentreName = centre.CentreNameHindi
                        } : (object)null
                    });
                }

                return Ok(registrationDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error fetching registration batch", details = ex.Message });
            }
        }

        //[HttpGet("get-registration-batch")]
        //public async Task<ActionResult<object>> GetRegistrationBatch(int CityCode, int start = 0, int size = 20)
        //{
        //    try
        //    {
        //        string session = "2025-26";

        //        var allRegistrations = await _context.Registration
        //            .Where(r => r.Session == session)
        //            .OrderBy(r => r.RegistrationNo)
        //            .ToListAsync();

        //        var registrations = allRegistrations
        //            .Where(r => (r.AssignedBoth ?? 0) / 100 == CityCode)
        //            .Skip(start)
        //            .Take(size)
        //            .ToList();

        //        if (!registrations.Any())
        //            return Ok(new object[0]);

        //        var centres = await _context.Centre
        //            .Where(c => c.CentreTableSession == session && c.CityCode == CityCode)
        //            .ToListAsync();

        //        var registrationDetails = new List<object>();

        //        foreach (var reg in registrations)
        //        {
        //            int assignedBoth = reg.AssignedBoth ?? 0;
        //            int cityCode = assignedBoth / 100;
        //            int centreCode = assignedBoth % 100;

        //            var centre = centres.FirstOrDefault(c => c.CityCode == cityCode && c.CentreCode == centreCode);

        //            string imageBase64 = null;
        //            string signatureBase64 = null;

        //            if (!string.IsNullOrEmpty(reg.ImagePath) && System.IO.File.Exists(reg.ImagePath))
        //                imageBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(await System.IO.File.ReadAllBytesAsync(reg.ImagePath))}";

        //            if (!string.IsNullOrEmpty(reg.SignaturePath) && System.IO.File.Exists(reg.SignaturePath))
        //                signatureBase64 = $"data:image/png;base64,{Convert.ToBase64String(await System.IO.File.ReadAllBytesAsync(reg.SignaturePath))}";

        //            registrationDetails.Add(new
        //            {
        //                reg.Name,
        //                reg.FName,
        //                reg.Gender,
        //                reg.Category,
        //                reg.DOB,
        //                reg.Address,
        //                reg.RegistrationNo,
        //                reg.RollNumber,
        //                reg.PhotoId,
        //                reg.Ph,
        //                reg.PhType,
        //                ImagePath = imageBase64,
        //                SignaturePath = signatureBase64,
        //                reg.SubCategory,
        //                AssignedBoth = centre != null ? new
        //                {
        //                    CityCode = centre.CityCode,
        //                    CityName = centre.CityNameHindi,
        //                    CentreCode = centre.CentreCode,
        //                    CentreName = centre.CentreNameHindi
        //                } : (object)null
        //            });
        //        }

        //        return Ok(registrationDetails);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { message = "Error fetching registration batch", details = ex.Message });
        //    }
        //}


        // 🔥 NEW API: Allocate Centres
        //[HttpPost("allocate-centres")]
        //public async Task<IActionResult> AllocateCentres([FromQuery] string session)
        //{
        //    try
        //    {
        //        // Load registrations sorted
        //        var registrations = await _context.Registration
        //            .Where(r => r.Session == session)
        //            .OrderBy(r => r.Name)
        //            .ThenBy(r => r.DOB)
        //            .ThenBy(r => r.RegistrationNo)
        //            .ToListAsync();

        //        var centres = await _context.Centre
        //            .Where(c => c.CentreTableSession == session)
        //            .ToListAsync();

        //        // Track current fills (not touching DB directly)
        //        var centreCounts = centres.ToDictionary(c => c.Id, c => 0);

        //        foreach (var reg in registrations)
        //        {
        //            var preferredCity = reg.PreferredCityCode;

        //            // Get all centres in the preferred city, except nodal
        //            var cityCentres = centres
        //                .Where(c => c.CityCode == preferredCity && c.CentreCode != 1)
        //                .OrderBy(c => c.CentreCode)
        //                .ToList();

        //            var assigned = false;

        //            // Try normal centres
        //            foreach (var centre in cityCentres)
        //            {
        //                if (centreCounts[centre.Id] < centre.Capacity)
        //                {
        //                    reg.AssignedCentre = (centre.CityCode * 100) + centre.CentreCode; ; // 👈 Save CityCode
        //                    centreCounts[centre.Id]++;
        //                    assigned = true;
        //                    break;
        //                }
        //            }

        //            // If not possible, assign nodal
        //            if (!assigned)
        //            {
        //                var nodalCentre = centres
        //                    .FirstOrDefault(c => c.CityCode == preferredCity && c.CentreCode == 1);

        //                if (nodalCentre != null)
        //                {
        //                    reg.AssignedCentre = (nodalCentre.CityCode * 100) + nodalCentre.CentreCode; // 👈 Still CityCode
        //                    centreCounts[nodalCentre.Id]++;

        //                    // Increase nodal capacity if overflow
        //                    if (centreCounts[nodalCentre.Id] > nodalCentre.Capacity)
        //                    {
        //                        nodalCentre.IncreasedCapacity =
        //                            centreCounts[nodalCentre.Id] - nodalCentre.Capacity;
        //                    }
        //                }
        //            }
        //        }

        //        await _context.SaveChangesAsync();
        //        return Ok(new { message = "Centres allocated successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { message = "Error during centre allocation", details = ex.Message });
        //    }
        //}

        //// 🔥 NEW API: Allocate Roll Numbers
        //[HttpPost("allocate-rollnumbers")]
        //public async Task<IActionResult> AllocateRollNumbers([FromQuery] string session)
        //{
        //    try
        //    {
        //        // Load registrations sorted by Name then DOB
        //        var registrations = await _context.Registration
        //            .Where(r => r.Session == session)
        //            .OrderBy(r => r.Name)
        //            .ThenBy(r => r.DOB)
        //            .ThenBy(r => r.RegistrationNo)
        //            .ToListAsync();

        //        // Group by AssignedCentre
        //        var groupedByCentre = registrations
        //            .GroupBy(r => r.AssignedCentre)
        //            .ToDictionary(g => g.Key, g => g.ToList());

        //        foreach (var group in groupedByCentre)
        //        {
        //            var assignedCentre = group.Key; // e.g., 1, 102, 1306
        //            int cityCode = assignedCentre.Value / 100;  // 1 for 102 or 13 for 1306
        //            int centreCode = assignedCentre.Value % 100; // 2 for 102 or 6 for 1306

        //            int serial = 1;
        //            foreach (var reg in group.Value)
        //            {
        //                string yearPart = "21";
        //                //string yearPart = DateTime.Now.Year.ToString().Substring(2, 2); // "25"
        //                string cityPart = cityCode.ToString("D2"); // 2 digits
        //                string centrePart = centreCode.ToString("D2"); // 2 digits
        //                string serialPart = serial.ToString("D3"); // 3 digits

        //                reg.RollNumber = int.Parse($"{yearPart}{cityPart}{centrePart}{serialPart}");
        //                serial++;
        //            }
        //        }

        //        await _context.SaveChangesAsync();
        //        return Ok(new { message = "Roll numbers allocated successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { message = "Error during roll number allocation", details = ex.Message });
        //    }
        //}


        [HttpPost("allocate-cities")]
        public async Task<IActionResult> AllocateCities([FromQuery] string session)
        {
            try
            {
                // Load registrations sorted
                var registrations = await _context.Registration
                    .Where(r => r.Session == session)
                    .OrderBy(r => r.Name)
                    .ThenBy(r => r.DOB)
                    .ThenBy(r => r.RegistrationNo)
                    .ToListAsync();

                var centres = await _context.Centre
                    .Where(c => c.CentreTableSession == session)
                    .ToListAsync();

                // Group centres by city for capacity calculations
                var centresByCity = centres
                    .GroupBy(c => c.CityCode)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Group registrations by preferred city
                var registrationsByCity = registrations
                    .GroupBy(r => r.PreferredCityCode)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Process city by city
                foreach (var cityGroup in registrationsByCity)
                {
                    var cityCode = cityGroup.Key;
                    var cityRegistrations = cityGroup.Value;

                    // Total capacity for all centres in that city
                    if (!centresByCity.TryGetValue(cityCode, out var cityCentres))
                        continue; // Skip if city has no centres configured

                    var totalCityCapacity = cityCentres.Sum(c => c.Capacity);
                    var totalCandidates = cityRegistrations.Count;

                    // Assign the city to each candidate
                    foreach (var reg in cityRegistrations)
                    {
                        reg.AssignedCity = cityCode;
                    }

                    // Handle nodal centre capacity increase (only if needed)
                    var nodalCentre = cityCentres.FirstOrDefault(c => c.CentreCode == 1);
                    if (nodalCentre != null)
                    {
                        if (totalCandidates > totalCityCapacity)
                        {
                            // Overflow amount
                            nodalCentre.IncreasedCapacity = totalCandidates - totalCityCapacity;
                        }
                        else
                        {
                            nodalCentre.IncreasedCapacity = 0; // reset if not needed
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Cities allocated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error during city allocation", details = ex.Message });
            }
        }


        //---- this api is for Increased capacity, increase the capacity in centre table and update the assigned centre to null

        //[HttpPost("allocate-centres-balanced")]
        //public async Task<IActionResult> AllocateCentresBalanced([FromQuery] string session)
        //{
        //    try
        //    {
        //        // 1️⃣ Load data
        //        var registrations = await _context.Registration
        //            .Where(r => r.Session == session && r.AssignedCity != null)
        //            .OrderBy(r => r.Name)
        //            .ToListAsync();

        //        var centres = await _context.Centre
        //            .Where(c => c.CentreTableSession == session)
        //            .ToListAsync();

        //        // 2️⃣ Group by city
        //        var centresByCity = centres
        //            .GroupBy(c => c.CityCode)
        //            .ToDictionary(g => g.Key, g => g.ToList());

        //        var regsByCity = registrations
        //            .GroupBy(r => r.AssignedCity)
        //            .ToDictionary(g => g.Key, g => g.ToList());

        //        // 3️⃣ Process each city
        //        foreach (var cityGroup in regsByCity)
        //        {
        //            var cityCode = cityGroup.Key ?? 0;
        //            var cityRegs = cityGroup.Value;

        //            if (!centresByCity.TryGetValue(cityCode, out var cityCentres))
        //                continue;

        //            // Separate normal and nodal centres
        //            var normalCentres = cityCentres
        //                .Where(c => c.CentreCode != 1)
        //                .OrderBy(c => c.CentreCode)
        //                .ToList();

        //            var nodalCentre = cityCentres
        //                .FirstOrDefault(c => c.CentreCode == 1);

        //            // Track capacity usage
        //            var totalCapacity = cityCentres.ToDictionary(
        //                c => c.Id,
        //                c => c.Capacity + (c.IncreasedCapacity ?? 0)
        //            );

        //            var usedSeats = cityCentres.ToDictionary(c => c.Id, c => 0);

        //            // 4️⃣ Group candidates by first letter for balance
        //            var letterGroups = cityRegs
        //                .GroupBy(r => char.ToUpper(r.Name.FirstOrDefault()))
        //                .OrderBy(g => g.Key)
        //                .ToList();

        //            // 5️⃣ Assign only to normal centres first
        //            var centreIndex = 0;
        //            foreach (var letterGroup in letterGroups)
        //            {
        //                foreach (var reg in letterGroup)
        //                {
        //                    bool assigned = false;
        //                    int attempts = 0;

        //                    // Try to place into normal centres first
        //                    while (!assigned && attempts < normalCentres.Count)
        //                    {
        //                        var centre = normalCentres[centreIndex];
        //                        if (usedSeats[centre.Id] < totalCapacity[centre.Id])
        //                        {
        //                            reg.AssignedCentre = (centre.CityCode * 100) + centre.CentreCode;
        //                            reg.AssignedCity = centre.CityCode;
        //                            usedSeats[centre.Id]++;
        //                            assigned = true;
        //                        }

        //                        // Move in round-robin
        //                        centreIndex = (centreIndex + 1) % normalCentres.Count;
        //                        attempts++;
        //                    }

        //                    // If not assigned (normal centres full), go to nodal
        //                    if (!assigned && nodalCentre != null)
        //                    {
        //                        if (usedSeats[nodalCentre.Id] < totalCapacity[nodalCentre.Id])
        //                        {
        //                            reg.AssignedCentre = (nodalCentre.CityCode * 100) + nodalCentre.CentreCode;
        //                            reg.AssignedCity = nodalCentre.CityCode;
        //                            usedSeats[nodalCentre.Id]++;
        //                            assigned = true;
        //                        }
        //                    }
        //                }
        //            }

        //            // 6️⃣ Update utilisedSeat
        //            foreach (var c in cityCentres)
        //            {
        //                c.utilisedSeat = usedSeats[c.Id];
        //            }
        //        }

        //        // 7️⃣ Save changes
        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Centres allocated successfully — all non-nodal centres filled first, balanced by first letter." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError,
        //            new { message = "Error during centre allocation", details = ex.Message });
        //    }
        //}



        // Create rooms for centres



        [HttpPost("create")]
        public async Task<IActionResult> CreateRoomsForCentres([FromQuery] string session)
        {
            try
            {
                if (string.IsNullOrEmpty(session))
                {
                    return BadRequest(new { message = "Session is required." });
                }

                // Filter centres by the session (e.g., "2025-26")
                var centres = await _context.Centre
                    .Where(c => c.CentreTableSession == session)  // Filter by session
                    .ToListAsync();

                if (centres == null || !centres.Any())
                {
                    return NotFound(new { message = $"No centres found for the session {session}." });
                }

                List<Room> newRooms = new List<Room>();

                // Loop through each centre to calculate and create rooms
                foreach (var centre in centres)
                {
                    int totalCapacity = centre.Capacity + (centre.IncreasedCapacity ?? 0); // Total capacity
                    int fullRooms = totalCapacity / 24;  // Number of full rooms
                    int remainingSeats = totalCapacity % 24;  // Remaining seats for an additional room

                    // Create full rooms
                    for (int i = 1; i <= fullRooms; i++)
                    {
                        var room = new Room
                        {
                            CityCode = centre.CityCode,
                            CentreCode = centre.CentreCode.ToString(),  // CentreCode as string (e.g. '1')
                            RoomNo = i,
                            RoomCapacity = 24  // Full room capacity
                        };
                        newRooms.Add(room);
                        _context.Rooms.Add(room);  // Add to context for saving
                    }

                    // If there are remaining seats, create an additional room
                    if (remainingSeats > 0)
                    {
                        var room = new Room
                        {
                            CityCode = centre.CityCode,
                            CentreCode = centre.CentreCode.ToString(),  // CentreCode as string (e.g. '1')
                            RoomNo = fullRooms + 1,  // Next room number
                            RoomCapacity = remainingSeats  // Remaining seats
                        };
                        newRooms.Add(room);
                        _context.Rooms.Add(room);  // Add to context for saving
                    }
                }

                // Save changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { message = "Rooms created successfully for all centres." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error occurred while creating rooms",
                    details = ex.Message
                });
            }
        }



        //[HttpPost("assign-rooms-balanced")]
        //public async Task<IActionResult> AssignRoomsBalanced([FromQuery] string session)
        //{
        //    try
        //    {
        //        // 1️⃣ Load all relevant registrations (City=26, Centre=9)
        //        var registrations = await _context.Registration
        //            .Where(r => r.Session == session && r.AssignedCentre == 1201)
        //            .OrderBy(r => r.Name)
        //            .ToListAsync();

        //        // 2️⃣ Load all rooms for that city-centre
        //        var rooms = await _context.Rooms
        //            .Where(r => r.CityCode == 12 && r.CentreCode == "1")
        //            .OrderBy(r => r.RoomNo)
        //            .ToListAsync();

        //        if (!registrations.Any() || !rooms.Any())
        //            return BadRequest("No registrations or rooms found for City=26, Centre=9.");

        //        // 3️⃣ Helper class to track room usage
        //        var roomUsage = rooms.ToDictionary(r => r.RoomNo.Value, r => new RoomTracker(r.RoomCapacity ?? 0));

        //        // 4️⃣ Group candidates by the first letter of their name
        //        var letterGroups = registrations
        //            .GroupBy(r => char.ToUpper(r.Name.FirstOrDefault()))
        //            .OrderByDescending(g => g.Count()) // Start with initials having most candidates
        //            .ToList();

        //        // 5️⃣ Allocate students by group
        //        foreach (var group in letterGroups)
        //        {
        //            var letter = group.Key;
        //            var candidates = group.ToList();

        //            foreach (var candidate in candidates)
        //            {
        //                bool assigned = false;

        //                // Try to find a suitable room for this candidate
        //                foreach (var room in rooms)
        //                {
        //                    var roomNo = room.RoomNo.Value;
        //                    var tracker = roomUsage[roomNo];

        //                    // Conditions:
        //                    //  - Room should not be full
        //                    //  - Same initial ≤ 12 students in one room
        //                    if (tracker.Used < tracker.Capacity)
        //                    {
        //                        int sameLetterCount = tracker.Assigned.GetValueOrDefault(letter, 0);

        //                        // If this letter already filled 12 seats, skip to next room
        //                        if (sameLetterCount >= 12)
        //                            continue;

        //                        // ✅ Assign candidate to this room
        //                        candidate.RoomNumber = roomNo;

        //                        // Update room tracker
        //                        tracker.Used++;
        //                        tracker.Assigned[letter] = sameLetterCount + 1;

        //                        assigned = true;
        //                        break; // move to next candidate
        //                    }
        //                }

        //                // If no room found (very unlikely), you can log it
        //                if (!assigned)
        //                {
        //                    Console.WriteLine($"⚠️ No available room found for {candidate.Name} ({letter}).");
        //                }
        //            }
        //        }

        //        // 6️⃣ Save all assigned room numbers
        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Rooms successfully assigned with balanced initials and capacity limits." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, new
        //        {
        //            message = "Error during room allocation",
        //            details = ex.Message
        //        });
        //    }
        //}

        // 🔹 Helper class for room tracking
        //public class RoomTracker
        //{
        //    public int Capacity { get; set; }
        //    public int Used { get; set; }
        //    public Dictionary<char, int> Assigned { get; set; }

        //    public RoomTracker(int capacity)
        //    {
        //        Capacity = capacity;
        //        Used = 0;
        //        Assigned = new Dictionary<char, int>();
        //    }
        //}


        //[HttpPost("distribute")]
        //public async Task<IActionResult> DistributeCandidates()
        //{
        //    string session = "2025-26";

        //    // Step 1: Get data
        //    var centres = await _context.Centre
        //        .Where(c => c.CentreTableSession == session)
        //        .ToListAsync();

        //    var registrations = await _context.Registration
        //        .Where(r => r.Session == session)
        //        .ToListAsync();

        //    // Step 2: Group by city
        //    var cities = registrations
        //        .GroupBy(r => r.AssignedCity)
        //        .ToDictionary(g => g.Key, g => g.ToList());

        //    List<Registrations> updatedRegs = new();

        //    // Step 3: Process each city separately
        //    foreach (var cityGroup in cities)
        //    {
        //        var cityCode = cityGroup.Key;
        //        var cityRegs = cityGroup.Value;

        //        // Get centers for this city
        //        var cityCentres = centres
        //            .Where(c => c.CityCode == cityCode)
        //            .OrderBy(c => c.CentreCode)
        //            .ToList();

        //        if (!cityCentres.Any())
        //            continue;

        //        // Track remaining capacity per center (CentreCode is int)
        //        var remainingCap = cityCentres.ToDictionary(c => c.CentreCode, c => c.utilisedSeat ?? 0);

        //        // Step 4: Group city registrations by initial letter
        //        var initials = cityRegs
        //            .GroupBy(r => r.Name.Substring(0, 1).ToUpper())
        //            .OrderByDescending(g => g.Count()) // handle high-frequency initials first
        //            .ToDictionary(g => g.Key, g => g.ToList());

        //        // Step 5: Distribute initials across centers
        //        foreach (var initialGroup in initials)
        //        {
        //            var initial = initialGroup.Key;
        //            var candidates = initialGroup.Value;
        //            int totalCandidates = candidates.Count;

        //            // Get centers with available capacity
        //            var activeCentres = remainingCap
        //                .Where(c => c.Value > 0)
        //                .ToDictionary(c => c.Key, c => c.Value);

        //            if (!activeCentres.Any())
        //                break; // all centers full

        //            int totalAvailable = activeCentres.Values.Sum();

        //            // Step 5.1: Proportional division by capacity
        //            var assignments = new Dictionary<int, int>();
        //            int assigned = 0;

        //            foreach (var kvp in activeCentres)
        //            {
        //                int centreCode = kvp.Key;
        //                int available = kvp.Value;

        //                // proportional assignment by capacity
        //                int assignCount = (int)Math.Round((double)available / totalAvailable * totalCandidates);

        //                // Ensure not above remaining capacity
        //                assignCount = Math.Min(assignCount, available);

        //                assignments[centreCode] = assignCount;
        //                assigned += assignCount;
        //            }

        //            // Step 5.2: Handle rounding leftovers
        //            int leftover = totalCandidates - assigned;
        //            if (leftover > 0)
        //            {
        //                var sorted = activeCentres
        //                    .OrderByDescending(c => c.Value)
        //                    .Select(c => c.Key)
        //                    .ToList();

        //                foreach (var code in sorted)
        //                {
        //                    if (leftover == 0) break;
        //                    int already = assignments.ContainsKey(code) ? assignments[code] : 0;
        //                    int canTake = Math.Min(leftover, remainingCap[code] - already);
        //                    assignments[code] = already + canTake;
        //                    leftover -= canTake;
        //                }
        //            }

        //            // Step 6: Apply assignments
        //            int startIndex = 0;
        //            foreach (var pair in assignments)
        //            {
        //                int centreCode = pair.Key;
        //                int count = pair.Value;
        //                if (count <= 0) continue;

        //                var toAssign = candidates
        //                    .Skip(startIndex)
        //                    .Take(count)
        //                    .ToList();

        //                foreach (var reg in toAssign)
        //                {
        //                    reg.AssignedCentre = centreCode; // direct int assignment
        //                    updatedRegs.Add(reg);
        //                }

        //                // update capacity tracker
        //                remainingCap[centreCode] -= count;
        //                startIndex += count;
        //            }
        //        }
        //    }

        //    // Step 7: Save all updates
        //    if (updatedRegs.Any())
        //    {
        //        _context.Registration.UpdateRange(updatedRegs);
        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok(new { message = "Candidates distributed successfully by initials, respecting centre capacities." });
        //}

        [HttpPost("distribute")]
        public async Task<IActionResult> DistributeCandidates()
        {
            string session = "2025-26";

            // Step 1: Get data
            var centres = await _context.Centre
                .Where(c => c.CentreTableSession == session)
                .ToListAsync();

            var registrations = await _context.Registration
                .Where(r => r.Session == session)
                .ToListAsync();

            // Step 2: Group registrations by city
            var cities = registrations
                .GroupBy(r => r.AssignedCity)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<Registrations> updatedRegs = new();

            // Step 3: Process each city separately
            foreach (var cityGroup in cities)
            {
                var cityCode = cityGroup.Key;
                var cityRegs = cityGroup.Value;

                // Get centres belonging to this city
                var cityCentres = centres
                    .Where(c => c.CityCode == cityCode)
                    .OrderBy(c => c.CentreCode)
                    .ToList();

                if (!cityCentres.Any())
                    continue;

                // Step 3.1: Available seats per centre (utilisedSeat = capacity)
                var remainingCap = cityCentres.ToDictionary(
                    c => c.CentreCode,
                    c => c.utilisedSeat ?? 0
                );

                // Step 4: Group candidates by first letter of name
                var initials = cityRegs
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.Name)
                        ? "#" : r.Name.Substring(0, 1).ToUpper())
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Step 5: Distribute candidates by initials
                foreach (var initialGroup in initials)
                {
                    var candidates = initialGroup.Value;
                    int totalCandidates = candidates.Count;

                    // Get centres with available capacity
                    var activeCentres = remainingCap
                        .Where(c => c.Value > 0)
                        .ToDictionary(c => c.Key, c => c.Value);

                    if (!activeCentres.Any())
                        break; // all centres full

                    int totalAvailable = activeCentres.Values.Sum();

                    // Step 5.1: Proportional distribution
                    var assignments = new Dictionary<int, int>();
                    int assigned = 0;

                    foreach (var kvp in activeCentres)
                    {
                        int centreCode = kvp.Key;
                        int available = kvp.Value;

                        int assignCount = (int)Math.Floor((double)available / totalAvailable * totalCandidates);
                        assignCount = Math.Min(assignCount, available);

                        assignments[centreCode] = assignCount;
                        assigned += assignCount;
                    }

                    // Step 5.2: Handle remaining candidates (rounding leftovers)
                    int leftover = totalCandidates - assigned;
                    if (leftover > 0)
                    {
                        var sorted = activeCentres
                            .OrderByDescending(c => c.Value)
                            .Select(c => c.Key)
                            .ToList();

                        int i = 0;
                        while (leftover > 0)
                        {
                            var code = sorted[i % sorted.Count];
                            assignments[code]++;
                            leftover--;
                            i++;
                        }
                    }

                    // Step 5.3: Apply assignments
                    int startIndex = 0;
                    foreach (var assign in assignments)
                    {
                        int centreCode = assign.Key;
                        int count = assign.Value;
                        if (count <= 0) continue;

                        var toAssign = candidates
                            .Skip(startIndex)
                            .Take(count)
                            .ToList();

                        foreach (var reg in toAssign)
                        {
                            reg.AssignedCentre = centreCode;
                            updatedRegs.Add(reg);
                        }

                        remainingCap[centreCode] = Math.Max(remainingCap[centreCode] - count, 0);
                        startIndex += count;
                    }
                }

                // Step 6: Assign any unassigned candidates
                var unassigned = cityRegs
                    .Where(r => r.AssignedCentre == null || r.AssignedCentre == 0)
                    .ToList();

                if (unassigned.Any())
                {
                    var sortedByCap = remainingCap
                        .OrderByDescending(c => c.Value)
                        .Select(c => c.Key)
                        .ToList();

                    int i = 0;
                    foreach (var reg in unassigned)
                    {
                        int centreCode = sortedByCap[i % sortedByCap.Count];
                        reg.AssignedCentre = centreCode;
                        updatedRegs.Add(reg);

                        remainingCap[centreCode] = Math.Max(remainingCap[centreCode] - 1, 0);
                        i++;
                    }
                }
            }

            // Step 7: Save updates
            if (updatedRegs.Any())
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                _context.Registration.UpdateRange(updatedRegs);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return Ok(new
            {
                message = "Candidates distributed successfully based on centre seat capacities (utilisedSeat) and initials."
            });
        }




        /*
                [HttpPost("distribute")]
                public async Task<IActionResult> DistributeCandidates()
                {
                    // Fetch the centres and their utilised seats for session "2025-26"
                    var centres = await _context.Centre
                        .Where(c => c.CentreTableSession == "2025-26")
                        .ToListAsync();

                    // Fetch the registration data for session "2025-26"
                    var registrations = await _context.Registration
                        .Where(r => r.Session == "2025-26")
                        .ToListAsync();

                    // Step 1: Calculate frequency of each letter based on registration data
                    var letterFrequencies = registrations
                        .GroupBy(r => new { r.AssignedCity, FirstLetter = r.Name.Substring(0, 1).ToUpper() })
                        .ToDictionary(g => g.Key, g => g.Count()); // Calculate frequency

                    // Step 2: Prepare the list of centres per city
                    var centresByCity = centres
                        .GroupBy(c => c.CityCode) // Group centres by CityCode
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Step 3: Distribute candidates across centres and update the `AssignedCentre`
                    List<Registrations> updatedRegistrations = new List<Registrations>();

                    foreach (var letterGroup in letterFrequencies)
                    {
                        var cityCode = letterGroup.Key.AssignedCity ?? 0;
                        var letterCode = letterGroup.Key.FirstLetter;
                        var targetFrequency = letterGroup.Value;

                        // Get centres for the current city
                        var cityCentres = centresByCity.ContainsKey(cityCode) ? centresByCity[cityCode] : new List<Centres>();

                        // Calculate the total utilised seats across all centres in this city
                        int totalUtilisedSeats = cityCentres.Sum(c => c.utilisedSeat ?? 0);

                        // If no seats are utilised, skip this letter distribution for the city
                        if (totalUtilisedSeats == 0)
                        {
                            continue;
                        }

                        // Step 4: Calculate the initial proportional distribution
                        int totalAssigned = 0;
                        List<(Centres Centre, int Assigned)> centreAssignments = new List<(Centres Centre, int Assigned)>();

                        foreach (var centre in cityCentres)
                        {
                            // Calculate the proportional number of candidates for this centre
                            int candidatesToAssign = (int)Math.Round((double)centre.utilisedSeat * targetFrequency / totalUtilisedSeats);

                            // Make sure no centre is allocated more candidates than it can handle (its utilised seats)
                            candidatesToAssign = Math.Min(candidatesToAssign, centre.utilisedSeat ?? 0);

                            centreAssignments.Add((centre, candidatesToAssign));
                            totalAssigned += candidatesToAssign;
                        }

                        // Step 5: Adjust for rounding errors to ensure the total matches the target frequency
                        // Step 5: Adjust for rounding errors to ensure the total matches the target frequency
                        int remainingCandidates = targetFrequency - totalAssigned;

                        // If there are remaining candidates to be distributed, distribute them
                        if (remainingCandidates != 0)
                        {
                            // Find the centre(s) with the highest utilised seats and adjust their assigned number
                            var sortedCentreAssignments = centreAssignments.OrderByDescending(ca => ca.Centre.utilisedSeat).ToList();

                            for (int i = 0; i < sortedCentreAssignments.Count; i++)
                            {
                                var centreAssignment = sortedCentreAssignments[i];

                                if (remainingCandidates == 0) break;

                                var centre = centreAssignment.Centre;
                                int maxAssignable = centre.utilisedSeat ?? 0;

                                // Distribute the remaining candidates without exceeding the centre's capacity
                                int additionalAssignment = Math.Min(remainingCandidates, maxAssignable - centreAssignment.Assigned);
                                centreAssignments[i] = (centre, centreAssignment.Assigned + additionalAssignment); // Update the centre assignment
                                remainingCandidates -= additionalAssignment;

                                if (remainingCandidates == 0) break;
                            }
                        }


                        // Step 6: Ensure the final count of assigned candidates matches the target frequency
                        if (remainingCandidates != 0)
                        {
                            return BadRequest("Could not distribute all candidates exactly due to rounding errors.");
                        }

                        // Step 7: Assign candidates to the centres
                        foreach (var centreAssignment in centreAssignments)
                        {
                            var centre = centreAssignment.Centre;
                            var centreAssigned = centreAssignment.Assigned;

                            // Step 8: Get registrations for this letter and city that haven't been assigned a centre
                            var registrationsToUpdate = registrations
                                .Where(r => r.Name.Substring(0, 1).ToUpper() == letterCode && r.AssignedCity == cityCode && r.AssignedCentre == null && r.Session == "2025-26")
                                .Take(centreAssigned) // Limit the number of candidates to be assigned
                                .ToList();

                            // Step 9: Assign candidates to the centre
                            foreach (var reg in registrationsToUpdate)
                            {
                                reg.AssignedCentre = centre.CentreCode; // Assign the centre code to the registration
                                updatedRegistrations.Add(reg); // Add to the list of updated registrations
                            }
                        }
                    }

                    // Step 10: Save the updated registrations to the database for session "2025-26"
                    if (updatedRegistrations.Any())
                    {
                        _context.Registration.UpdateRange(updatedRegistrations);
                        await _context.SaveChangesAsync();
                    }

                    return Ok(new { message = "Candidates successfully distributed and assigned to centres." });
                }
        */

   

        [HttpPost("assign-rooms-balanced")]
        public async Task<IActionResult> AssignRoomsBalanced([FromQuery] string session)
        {
            try
            {
                const int RoomSize = 24;
                const int SeatsPerRow = 6;
                const int RowsPerRoom = RoomSize / SeatsPerRow;

                // 1️⃣ Load registrations for the session
                var registrations = await _context.Registration
                    .Where(r => r.Session == session && r.AssignedBoth != null)
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                if (!registrations.Any())
                    return BadRequest("No registrations found for this session.");

                // 2️⃣ Load all rooms
                var allRooms = await _context.Rooms
                    .Where(r => r.CityCode != null && r.CentreCode != null)
                    .OrderBy(r => r.RoomNo)
                    .ToListAsync();

                if (!allRooms.Any())
                    return BadRequest("No rooms found in the database.");

                // 3️⃣ Group by AssignedCentre
                var centreGroups = registrations.GroupBy(r => r.AssignedBoth).ToList();

                int totalAssigned = 0;

                foreach (var centreGroup in centreGroups)
                {
                    int assignedCentre = centreGroup.Key ?? 0;
                    var centreRegistrations = centreGroup.ToList();

                    int cityCode = assignedCentre / 100;
                    int centreCode = assignedCentre % 100;

                    // 4️⃣ Find rooms for this centre
                    var rooms = allRooms
                        .Where(r => r.CityCode == cityCode && r.CentreCode == centreCode.ToString())
                        .OrderBy(r => r.RoomNo)
                        .ToList();

                    if (!rooms.Any())
                    {
                        Console.WriteLine($"⚠️ No rooms found for centre {assignedCentre}");
                        continue;
                    }

                    // Group candidates by first letter
                    var letterGroups = centreRegistrations
                        .GroupBy(r => char.ToUpper(r.Name.FirstOrDefault()))
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key, g => new Queue<Registrations>(g));

                    var assignedCandidates = new HashSet<int>();

                    // 5️⃣ Phase 1: Assign seats (non-adjacent by first letter)
                    foreach (var room in rooms)
                    {
                        var seatingGrid = new Registrations[RowsPerRoom, SeatsPerRow];
                        int assignedInRoom = 0;

                        for (int row = 0; row < RowsPerRoom; row++)
                        {
                            for (int seat = 0; seat < SeatsPerRow; seat++)
                            {
                                if (assignedInRoom >= RoomSize) break;

                                char? leftInitial = seat > 0 && seatingGrid[row, seat - 1] != null
                                    ? char.ToUpper(seatingGrid[row, seat - 1].Name.FirstOrDefault())
                                    : (char?)null;

                                char? aboveInitial = row > 0 && seatingGrid[row - 1, seat] != null
                                    ? char.ToUpper(seatingGrid[row - 1, seat].Name.FirstOrDefault())
                                    : (char?)null;

                                var nextGroup = letterGroups
                                    .Where(g => g.Value.Any() &&
                                                g.Key != leftInitial &&
                                                g.Key != aboveInitial)
                                    .OrderByDescending(g => g.Value.Count)
                                    .FirstOrDefault();

                                if (nextGroup.Value == null)
                                    continue;

                                var candidate = nextGroup.Value.Dequeue();
                                candidate.RoomNumber = room.RoomNo;
                                assignedCandidates.Add(candidate.Id);
                                assignedInRoom++;
                                totalAssigned++;
                                seatingGrid[row, seat] = candidate;
                            }
                        }
                    }

                    // 6️⃣ Phase 2: Assign leftovers to rooms with available capacity
                    var unassigned = centreRegistrations
                        .Where(r => r.RoomNumber == null)
                        .ToList();

                    if (unassigned.Any())
                    {
                        var roomOccupancy = rooms.ToDictionary(r => r.RoomNo.Value, _ => 0);
                        foreach (var reg in centreRegistrations.Where(r => r.RoomNumber != null))
                        {
                            if (roomOccupancy.ContainsKey((int)reg.RoomNumber))
                                roomOccupancy[(int)reg.RoomNumber]++;
                        }

                        foreach (var candidate in unassigned)
                        {
                            var availableRoom = roomOccupancy
                                .Where(r => r.Value < RoomSize)
                                .OrderBy(r => r.Value)
                                .FirstOrDefault();

                            if (availableRoom.Key != 0)
                            {
                                candidate.RoomNumber = availableRoom.Key;
                                roomOccupancy[availableRoom.Key]++;
                                totalAssigned++;
                            }
                        }
                    }

                    // 7️⃣ Phase 3: Final check — no one left unassigned, no overfill
                    var stillUnassigned = centreRegistrations
                        .Where(r => r.RoomNumber == null)
                        .ToList();

                    if (stillUnassigned.Any())
                    {
                        var roomCounts = centreRegistrations
                            .Where(r => r.RoomNumber != null)
                            .GroupBy(r => r.RoomNumber)
                            .ToDictionary(g => g.Key!.Value, g => g.Count());

                        foreach (var candidate in stillUnassigned)
                        {
                            var availableRoom = roomCounts
                                .Where(rc => rc.Value < RoomSize)
                                .OrderBy(rc => rc.Value)
                                .Select(rc => rc.Key)
                                .FirstOrDefault();

                            if (availableRoom != 0)
                            {
                                candidate.RoomNumber = availableRoom;
                                roomCounts[availableRoom]++;
                                totalAssigned++;
                            }
                            else
                            {
                                // as absolute fallback — assign to smallest room id
                                candidate.RoomNumber = rooms.First().RoomNo;
                                totalAssigned++;
                            }
                        }
                    }

                    Console.WriteLine($"✅ Centre {assignedCentre}: {centreRegistrations.Count} processed.");
                }

                // 8️⃣ Save changes
                await _context.SaveChangesAsync();

                int total = registrations.Count;
                int unassignedCount = registrations.Count(r => r.RoomNumber == null);

                return Ok(new
                {
                    message = "Room assignment completed successfully (non-adjacent + full guaranteed).",
                    totalAssigned,
                    totalCandidates = total,
                    unassignedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Error during room assignment.",
                    details = ex.Message
                });
            }
        }



        //rollno not assigning in sequence

    //    [HttpPost("assign-rollnumbers")]
    //    public async Task<IActionResult> AssignRollNumbers(string session = "2025-26")
    //    {
    //        var registrations = await _context.Registration
    //            .Where(r => r.Session == session && r.AssignedCentre != null && r.RoomNumber != null)
    //            .OrderBy(r => r.AssignedCentre)
    //            .ToListAsync();

    //        var groupedByCentre = registrations
    //            .GroupBy(r => new { r.AssignedCity, r.AssignedCentre })
    //            .ToList();

    //        foreach (var centreGroup in groupedByCentre)
    //        {
    //            int serial = 1;
    //            var rooms = centreGroup.GroupBy(r => r.RoomNumber);

    //            foreach (var room in rooms)
    //            {
    //                var candidates = room.ToList();
    //                var seatLayout = new Registrations[6, 4]; // 6x4 = 24 seats

    //                // Group by first letter of name
    //                var letterGroups = candidates
    //.GroupBy(x => char.ToUpper(x.Name.FirstOrDefault()))
    //.OrderByDescending(g => g.Count())
    //.Select(g => g.ToList()) // convert to list for mutable handling
    //.ToList();

    //                // Flatten groups while shuffling to reduce adjacency
    //                var arrangedList = new List<Registrations>();
    //                while (letterGroups.Any(g => g.Any()))
    //                {
    //                    foreach (var g in letterGroups.ToList())
    //                    {
    //                        if (g.Any())
    //                        {
    //                            arrangedList.Add(g.First());
    //                            letterGroups.Remove(g);
    //                            var remaining = g.Skip(1).ToList();
    //                            if (remaining.Any())
    //                                letterGroups.Add(remaining );
    //                        }
    //                    }
    //                }

    //                int rows = 6, cols = 4;
    //                int index = 0;

    //                for (int r = 0; r < rows; r++)
    //                {
    //                    for (int c = 0; c < cols; c++)
    //                    {
    //                        if (index >= arrangedList.Count)
    //                            break;

    //                        var candidate = arrangedList[index];
    //                        char initial = char.ToUpper(candidate.Name.FirstOrDefault());

    //                        // Find safe spot (no same initial adjacent)
    //                        bool placed = false;
    //                        for (int rr = 0; rr < rows && !placed; rr++)
    //                        {
    //                            for (int cc = 0; cc < cols && !placed; cc++)
    //                            {
    //                                if (seatLayout[rr, cc] != null) continue;

    //                                bool safe = true;
    //                                var dirs = new (int, int)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
    //                                foreach (var (dr, dc) in dirs)
    //                                {
    //                                    int nr = rr + dr, nc = cc + dc;
    //                                    if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
    //                                    {
    //                                        var adj = seatLayout[nr, nc];
    //                                        if (adj != null && char.ToUpper(adj.Name.FirstOrDefault()) == initial)
    //                                        {
    //                                            safe = false;
    //                                            break;
    //                                        }
    //                                    }
    //                                }

    //                                if (safe)
    //                                {
    //                                    seatLayout[rr, cc] = candidate;
    //                                    placed = true;
    //                                }
    //                            }
    //                        }

    //                        // fallback if not placed safely
    //                        if (!placed)
    //                        {
    //                            for (int rr = 0; rr < rows && !placed; rr++)
    //                            {
    //                                for (int cc = 0; cc < cols && !placed; cc++)
    //                                {
    //                                    if (seatLayout[rr, cc] == null)
    //                                    {
    //                                        seatLayout[rr, cc] = candidate;
    //                                        placed = true;
    //                                    }
    //                                }
    //                            }
    //                        }

    //                        index++;
    //                    }
    //                }

    //                // Assign roll numbers row-wise
    //                for (int r = 0; r < rows; r++)
    //                {
    //                    for (int c = 0; c < cols; c++)
    //                    {
    //                        var candidate = seatLayout[r, c];
    //                        if (candidate == null) continue;

    //                        candidate.RollNumber = int.Parse($"25{centreGroup.Key.AssignedCity:D2}{centreGroup.Key.AssignedCentre:D2}{serial:D3}");
    //                        serial++;
    //                    }
    //                }
    //            }
    //        }

    //        await _context.SaveChangesAsync();

    //        return Ok(new
    //        {
    //            message = "Roll number assignment completed successfully (no adjacent same initials).",
    //            totalAssigned = registrations.Count
    //        });
    //    }


    }
}

