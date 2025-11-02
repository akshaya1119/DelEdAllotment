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
    public class CentresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CentresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Centres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Centres>>> GetCentre()
        {
            return await _context.Centre.ToListAsync();
        }

        // GET: api/Centres/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Centres>> GetCentres(int id)
        {
            var centres = await _context.Centre.FindAsync(id);

            if (centres == null)
            {
                return NotFound();
            }

            return centres;
        }

        // PUT: api/Centres/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCentres(int id, Centres centres)
        {
            if (id != centres.Id)
            {
                return BadRequest();
            }

            _context.Entry(centres).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CentresExists(id))
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

        // POST: api/Centres
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Centres>> PostCentres(Centres centres)
        {
            _context.Centre.Add(centres);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCentres", new { id = centres.Id }, centres);
        }

        // DELETE: api/Centres/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCentres(int id)
        {
            var centres = await _context.Centre.FindAsync(id);
            if (centres == null)
            {
                return NotFound();
            }

            _context.Centre.Remove(centres);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CentresExists(int id)
        {
            return _context.Centre.Any(e => e.Id == id);
        }


        [HttpPost("Allocate")]
        public async Task<ActionResult<IEnumerable<RoomAllocationResponse>>> AllocateRooms()
        {
            // Fetch Centres, Rooms, and Registrations from the database where Session is "2025-26"
            var centres = await _context.Centre.Where(c => c.CentreTableSession == "2025-26").ToListAsync();
            var rooms = await _context.Rooms.ToListAsync();
            var registrations = await _context.Registration
                .Where(r => r.Session == "2025-26")  // Filter registrations by session
                .ToListAsync();

            // Step 1: Calculate the frequency of the first letter of `Name` (grouped by `AssignCity`) from the Registration table
            var letterFrequencyByCity = CalculateFirstLetterFrequencyFromRegistrations(registrations);

            // Step 2: Perform room allocation based on the frequency calculation
            var roomAllocations = AllocateRoomsLogic(centres, rooms, letterFrequencyByCity);

            // Return the room allocation results
            return Ok(roomAllocations.Values);
        }

        #region Helper Methods

        // Method to calculate first letter frequency of `Name` in the Registration table, grouped by `AssignCity`
        private Dictionary<string, Dictionary<char, int>> CalculateFirstLetterFrequencyFromRegistrations(List<Registrations> registrations)
        {
            var cityLetterFrequency = new Dictionary<string, Dictionary<char, int>>();

            foreach (var reg in registrations)
            {
                // Extract the first letter from the `Name` field
                char firstLetter = char.ToUpper(reg.Name[0]);

                // Group by `AssignCity`
                if (!cityLetterFrequency.ContainsKey(reg.AssignedCity.ToString()))
                {
                    cityLetterFrequency[reg.AssignedCity.ToString()] = new Dictionary<char, int>();
                }

                var cityFrequency = cityLetterFrequency[reg.AssignedCity.ToString()];

                if (cityFrequency.ContainsKey(firstLetter))
                    cityFrequency[firstLetter]++;
                else
                    cityFrequency[firstLetter] = 1;
            }

            return cityLetterFrequency;
        }

        // Room allocation logic based on the constraints
        private Dictionary<int, RoomAllocationResponse> AllocateRoomsLogic(
            List<Centres> centres,
            List<Room> rooms,
            Dictionary<string, Dictionary<char, int>> letterFrequencyByCity)
        {
            var allocations = new Dictionary<int, RoomAllocationResponse>();

            // Step 3: Perform room allocation per city
            foreach (var cityLetterFrequency in letterFrequencyByCity)
            {
                var city = cityLetterFrequency.Key;
                var letterFrequency = cityLetterFrequency.Value;

                // Find all centres for this city
                var cityCentres = centres.Where(c => c.CityCode == int.Parse(city)).OrderBy(c => c.CentreCode == 1 ? 1 : 0).ToList();

                // Allocate rooms from Centre 2 first, then Centre 1 if needed
                foreach (var centre in cityCentres)
                {
                    // Get all rooms for this centre
                    var availableRooms = rooms.Where(r => r.CentreCode.ToString() == centre.CentreCode.ToString()).ToList();

                    foreach (var room in availableRooms)
                    {
                        // Check if any letter exceeds 12 occurrences in this room
                        if (DoesRoomExceedLetterLimit(room.RoomNo ?? 0, letterFrequency))
                        {
                            // If the letter frequency exceeds 12, allocate it to Centre 1
                            var centre1 = centres.First(c => c.CentreCode == 1 && c.CityCode == int.Parse(city));
                            allocations[room.RoomNo ?? 0] = new RoomAllocationResponse
                            {
                                RoomNo = room.RoomNo ?? 0,
                                CityName = city,
                                CentreName = centre1.CentreName
                            };

                            // Update Registration with Centre 1 allocation
                            UpdateRegistrationForRoomAllocation(city, room.RoomNo ?? 0, centre1);
                            break;  // No need to allocate more rooms once one is allocated to Centre 1
                        }
                        else
                        {
                            // Allocate the room if letter limit is satisfied
                            allocations[room.RoomNo ?? 0] = new RoomAllocationResponse
                            {
                                RoomNo = room.RoomNo ?? 0,
                                CityName = city,
                                CentreName = centre.CentreName
                            };

                            // Update Registration with the allocated room and centre
                            UpdateRegistrationForRoomAllocation(city, room.RoomNo ?? 0, centre);
                        }
                    }
                }
            }

            // Save changes to the database after allocations
            _context.SaveChanges();

            return allocations;
        }

        // Method to check if any letter exceeds 12 occurrences in a room allocation
        private bool DoesRoomExceedLetterLimit(int roomNo, Dictionary<char, int> letterFrequency)
        {
            foreach (var letter in letterFrequency)
            {
                if (letter.Value > 12)
                    return true;  // Exceeds limit
            }
            return false;  // No letter exceeds the limit
        }

        // Method to update the `RoomNo` and `AssignCentre` in the Registration table for the allocated room
        // Method to update the `RoomNo` and `AssignCentre` in the Registration table for the allocated room
        private async Task UpdateRegistrationForRoomAllocation(string city, int roomNo, Centres centre)
        {
            // Ensure city parsing is correct, and that the comparison is accurate for both the city and centre name
            var registrationToAssign = await _context.Registration
    .Where(r => r.AssignedCity.ToString() == city && r.Name.StartsWith(centre.CentreName[0].ToString())) // Convert AssignedCity to string
    .FirstOrDefaultAsync();

            if (registrationToAssign != null)
            {
                registrationToAssign.RoomNumber = roomNo;
                registrationToAssign.AssignedCentre = centre.CentreCode;

                // Mark the entity as modified so that EF tracks the changes
                _context.Entry(registrationToAssign).State = EntityState.Modified;

                // Save the changes to the database
                await _context.SaveChangesAsync(); // Ensure async saving of changes
            }
        }

        // Make sure to call this updated method in your room allocation logic.

        #endregion

        public class RoomAllocationResponse
        {
            public int RoomNo { get; set; }
            public string CityName { get; set; }
            public string CentreName { get; set; }
        }



    }
}
