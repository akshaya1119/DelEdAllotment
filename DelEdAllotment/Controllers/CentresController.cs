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
    }
}
