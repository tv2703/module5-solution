using UserManagementAPI.Models;

namespace UserManagementAPI.Repositories
{
    /// <summary>
    /// In-memory repository that stores and manages user records.
    ///
    /// Bug fixes applied (Copilot analysis):
    ///   - BUG 1: List&lt;T&gt; is not thread-safe. Added a lock object so concurrent
    ///            requests cannot corrupt the list during simultaneous reads/writes.
    ///   - BUG 2: GetAll() returned the live internal list reference. Any caller
    ///            could mutate it from outside. Now returns a safe snapshot copy.
    ///   - BUG 3: Create/Update did not trim whitespace from string fields,
    ///            so "  Alice  " was stored as-is. Fields are now trimmed before saving.
    /// </summary>
    public class UserRepository
    {
        // BUG FIX 1: lock object for thread-safe list access
        private readonly object _lock = new();
        private readonly List<User> _users = new();
        private int _nextId = 1;

        public UserRepository()
        {
            // Seed with sample data so the API has initial records to work with
            _users.AddRange(new[]
            {
                new User { Id = _nextId++, FirstName = "Alice",  LastName = "Johnson",  Email = "alice.johnson@techhive.com",  Department = "HR",          Role = "HR Manager",        CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new User { Id = _nextId++, FirstName = "Bob",    LastName = "Smith",    Email = "bob.smith@techhive.com",      Department = "IT",          Role = "System Admin",      CreatedAt = DateTime.UtcNow.AddDays(-20) },
                new User { Id = _nextId++, FirstName = "Carol",  LastName = "White",    Email = "carol.white@techhive.com",    Department = "Finance",     Role = "Analyst",           CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new User { Id = _nextId++, FirstName = "David",  LastName = "Brown",    Email = "david.brown@techhive.com",    Department = "Engineering", Role = "Software Engineer", CreatedAt = DateTime.UtcNow.AddDays(-5)  },
                new User { Id = _nextId++, FirstName = "Eva",    LastName = "Martinez", Email = "eva.martinez@techhive.com",   Department = "Marketing",   Role = "Marketing Lead",    CreatedAt = DateTime.UtcNow.AddDays(-2)  },
            });
        }

        /// <summary>
        /// Returns a snapshot copy of all users.
        /// BUG FIX 2: Previously returned the live list reference, allowing
        /// callers to mutate internal state from outside the repository.
        /// </summary>
        public IEnumerable<User> GetAll()
        {
            lock (_lock)
            {
                // Return a shallow copy — safe snapshot
                return _users.ToList();
            }
        }

        /// <summary>Returns a single user by ID, or null if not found.</summary>
        public User? GetById(int id)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.Id == id);
            }
        }

        /// <summary>
        /// Creates a new user from the request DTO and returns it.
        /// BUG FIX 3: All string fields are now trimmed before storage.
        /// </summary>
        public User Create(CreateUserRequest request)
        {
            var user = new User
            {
                Id         = 0, // assigned under lock below
                // BUG FIX 3: Trim whitespace from all string inputs
                FirstName  = request.FirstName.Trim(),
                LastName   = request.LastName.Trim(),
                Email      = request.Email.Trim().ToLowerInvariant(),
                Department = request.Department.Trim(),
                Role       = request.Role.Trim(),
                CreatedAt  = DateTime.UtcNow,
                IsActive   = true
            };

            lock (_lock)
            {
                user.Id = _nextId++;
                _users.Add(user);
            }

            return user;
        }

        /// <summary>
        /// Updates an existing user.
        /// BUG FIX 3: All string fields are trimmed before updating.
        /// Returns false if the user does not exist.
        /// </summary>
        public bool Update(int id, UpdateUserRequest request)
        {
            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                if (user is null) return false;

                // BUG FIX 3: Trim whitespace before saving
                user.FirstName  = request.FirstName.Trim();
                user.LastName   = request.LastName.Trim();
                user.Email      = request.Email.Trim().ToLowerInvariant();
                user.Department = request.Department.Trim();
                user.Role       = request.Role.Trim();
                user.IsActive   = request.IsActive;
                return true;
            }
        }

        /// <summary>
        /// Deletes a user by ID.
        /// Returns false if the user does not exist.
        /// </summary>
        public bool Delete(int id)
        {
            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                if (user is null) return false;
                _users.Remove(user);
                return true;
            }
        }

        /// <summary>
        /// Checks whether an email is already taken (optionally excluding a specific user ID).
        /// Email comparison is case-insensitive.
        /// </summary>
        public bool EmailExists(string email, int? excludeId = null)
        {
            var normalised = email.Trim().ToLowerInvariant();
            lock (_lock)
            {
                return _users.Any(u =>
                    u.Email.Equals(normalised, StringComparison.OrdinalIgnoreCase)
                    && (excludeId is null || u.Id != excludeId));
            }
        }
    }
}
