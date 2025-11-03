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
        public async Task<ActionResult<IEnumerable<Centres>>> GetCentre([FromQuery]  int citycode, string session = "2025-26")
        {
            if (string.IsNullOrEmpty(session))
            {
                return BadRequest("Session is required.");
            }

            // Example: filter centres by a session string value
            var centres = await _context.Centre
                .Where(c => c.CentreTableSession == session && c.CityCode == citycode) // Assuming your Centres table has a Session column
                .ToListAsync();

            return centres;
        }

        [HttpGet("get-cities")]
        public async Task<ActionResult<IEnumerable<Centres>>> GetCity([FromQuery] string session = "2025-26")
        {
            if (string.IsNullOrEmpty(session))
            {
                return BadRequest("Session is required.");
            }

            // Example: filter centres by a session string value
            var cities = await _context.Centre
                .Where(c => c.CentreTableSession == session) // Assuming your Centres table has a Session column
                  .Select(c => new
                  {
                      c.CityCode,
                      c.CityNameHindi
                  })
        .Distinct()
                .ToListAsync();

            if (!cities.Any())
            {
                return NotFound("No cities found for the given session.");
            }

            return Ok(cities);
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



        // ✅ GET: api/SeatAllotment/by-room?cityCode=1&centerCode=101&roomNumber=5
        [HttpGet("by-room")]
        public async Task<IActionResult> GetCandidatesByRoom(
            [FromQuery] int cityCode,
            [FromQuery] int centerCode,
            [FromQuery] int roomNumber)
        {
            // Validate inputs
            if (cityCode <= 0 || centerCode <= 0 || roomNumber <= 0)
            {
                return BadRequest(new { message = "cityCode, centerCode, and roomNumber are required and must be greater than 0." });
            }

            // Fetch all candidates for given room
            var candidates = await _context.seat_allotments
                .Where(x => x.city_code == cityCode
                            && x.center_code == centerCode
                            && x.room_number == roomNumber)
                .OrderBy(x => x.seat_row)
                .ThenBy(x => x.seat_number)
                .Select(x => new
                {
                    x.registration_no,
                    x.name,
                    x.center_id,
                    x.room_number,
                    x.seat_row,
                    x.seat_number,
                    x.allotment_date,
                    x.city_code,
                    x.center_code,
                    x.roll_no
                })
                .ToListAsync();

            if (!candidates.Any())
            {
                return NotFound(new
                {
                    message = $"No candidates found for CityCode={cityCode}, CentreCode={centerCode}, RoomNumber={roomNumber}"
                });
            }

            return Ok(candidates);
        }

        // ✅ GET: api/Rooms
        [HttpGet("get-all-rooms")]
        public async Task<IActionResult> GetAllRooms()
        {
            var rooms = await _context.Rooms
                .OrderBy(r => r.CityCode)
                .ThenBy(r => r.CentreCode)
                .ThenBy(r => r.RoomNo)
                .ToListAsync();

            return Ok(rooms);
        }

        // ✅ GET: api/Rooms/by-centre?cityCode=1&centreCode=1
        [HttpGet("get-rooms-by-city&centre")]
        public async Task<IActionResult> GetRooms(int cityCode, int centreCode)
        {
            string centreCodeStr = centreCode.ToString();

            var rooms = await _context.Rooms
                .Where(r => r.CityCode == cityCode && r.CentreCode == centreCodeStr)
                .OrderBy(r => r.RoomNo)
                .ToListAsync();

            if (!rooms.Any())
                return NotFound("No rooms found for the given city and centre codes.");

            return Ok(rooms);
        }


    }
}
