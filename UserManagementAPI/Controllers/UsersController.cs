using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Controllers
{
    /// <summary>
    /// Handles all CRUD operations for User records.
    ///
    /// Bug fixes applied (Copilot analysis):
    ///   - BUG 4: No ID range validation — negative/zero IDs were accepted silently.
    ///            Added a guard that returns 400 Bad Request for id &lt;= 0.
    ///   - BUG 5: No try-catch blocks — any unhandled exception propagated as a raw
    ///            500 response, leaking stack traces. All actions are now wrapped in
    ///            try-catch and return a structured 500 error message.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _repo;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserRepository repo, ILogger<UsersController> logger)
        {
            _repo   = repo;
            _logger = logger;
        }

        // ─────────────────────────────────────────────
        // GET /api/users
        // ─────────────────────────────────────────────
        /// <summary>Retrieves all users.</summary>
        /// <returns>A list of all user records.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<User>> GetAll()
        {
            // BUG FIX 5: Wrap in try-catch to prevent unhandled exceptions crashing the API
            try
            {
                var users = _repo.GetAll();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAll.");
                return StatusCode(500, new { message = "An unexpected error occurred while retrieving users." });
            }
        }

        // ─────────────────────────────────────────────
        // GET /api/users/{id}
        // ─────────────────────────────────────────────
        /// <summary>Retrieves a specific user by ID.</summary>
        /// <param name="id">The unique identifier of the user (must be &gt; 0).</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<User> GetById(int id)
        {
            // BUG FIX 4: Reject invalid IDs (0 or negative) — previously silently accepted
            if (id <= 0)
                return BadRequest(new { message = "User ID must be a positive integer." });

            try
            {
                var user = _repo.GetById(id);
                if (user is null)
                    return NotFound(new { message = $"User with ID {id} was not found." });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetById for ID {Id}.", id);
                return StatusCode(500, new { message = "An unexpected error occurred while retrieving the user." });
            }
        }

        // ─────────────────────────────────────────────
        // POST /api/users
        // ─────────────────────────────────────────────
        /// <summary>Creates a new user.</summary>
        /// <param name="request">User creation payload.</param>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<User> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Check for duplicate email
                if (_repo.EmailExists(request.Email))
                    return Conflict(new { message = $"A user with email '{request.Email.Trim()}' already exists." });

                var user = _repo.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Create.");
                return StatusCode(500, new { message = "An unexpected error occurred while creating the user." });
            }
        }

        // ─────────────────────────────────────────────
        // PUT /api/users/{id}
        // ─────────────────────────────────────────────
        /// <summary>Updates an existing user's details.</summary>
        /// <param name="id">The unique identifier of the user to update (must be &gt; 0).</param>
        /// <param name="request">Updated user details.</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<User> Update(int id, [FromBody] UpdateUserRequest request)
        {
            // BUG FIX 4: Reject invalid IDs
            if (id <= 0)
                return BadRequest(new { message = "User ID must be a positive integer." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Check for duplicate email (excluding the current user)
                if (_repo.EmailExists(request.Email, excludeId: id))
                    return Conflict(new { message = $"Email '{request.Email.Trim()}' is already in use by another user." });

                var updated = _repo.Update(id, request);
                if (!updated)
                    return NotFound(new { message = $"User with ID {id} was not found." });

                return Ok(_repo.GetById(id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Update for ID {Id}.", id);
                return StatusCode(500, new { message = "An unexpected error occurred while updating the user." });
            }
        }

        // ─────────────────────────────────────────────
        // DELETE /api/users/{id}
        // ─────────────────────────────────────────────
        /// <summary>Removes a user by ID.</summary>
        /// <param name="id">The unique identifier of the user to delete (must be &gt; 0).</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult Delete(int id)
        {
            // BUG FIX 4: Reject invalid IDs
            if (id <= 0)
                return BadRequest(new { message = "User ID must be a positive integer." });

            try
            {
                var deleted = _repo.Delete(id);
                if (!deleted)
                    return NotFound(new { message = $"User with ID {id} was not found." });

                return Ok(new { message = $"User with ID {id} was successfully deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Delete for ID {Id}.", id);
                return StatusCode(500, new { message = "An unexpected error occurred while deleting the user." });
            }
        }
    }
}
