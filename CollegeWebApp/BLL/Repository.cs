using CollegeWebApp.Models;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace CollegeWebApp.BLL
{
    public class Repository
    {
        private readonly IConfiguration _configuration;
        private readonly string connString;

        public Repository(IConfiguration configuration)
        {
            _configuration = configuration;
            connString = _configuration.GetConnectionString("Default");
        }

        public async Task<UserDTO?> GetUserIfExistAsync(string email, string password)
        {

            string query = @"
                    SELECT 
                  Users.Id, 
                  Users.Email, 
                  Users.PasswordHash, 
                  Users.FirstName, 
                  Users.Surname, 
                  Users.Patronymic, 
                  Users.ProfilePic,
                  Users.GroupId,
                  Permits.ScheduleManagement, 
                  Permits.UserManagement, 
                  Permits.NewsManagement, 
                  Permits.BooksManagment
                FROM Users 
                JOIN UserRoles ON Users.Id = UserRoles.UserId
                JOIN Roles ON UserRoles.RoleId = Roles.Id
                JOIN Permits ON Roles.Id = Permits.RoleId
                WHERE Email = @emailVal 
                  AND PasswordHash = @passVal
                ";

            UserDTO user = new UserDTO();

            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@emailVal", email);
                    string passHash = Helpers.GeneratePasswordHash(password);
                    command.Parameters.AddWithValue("@passVal", passHash);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            await connection.CloseAsync();
                            return null;
                        }

                        while (await reader.ReadAsync())
                        {
                            user.Id = Int32.Parse(reader["Id"].ToString());
                            user.Email = reader["Email"].ToString();
                            if (reader["PasswordHash"].ToString() != passHash)
                            {
                                await connection.CloseAsync();
                                return null;
                            }
                            user.PasswordHash = reader["PasswordHash"].ToString();
                            user.FirstName = reader["FirstName"].ToString();
                            user.Surname = reader["Surname"].ToString();
                            user.Patronymic = reader["Patronymic"].ToString();
                            user.ProfilePic = reader["ProfilePic"].ToString();
                            object groupIdValue = reader["GroupId"];
                            user.GroupId = groupIdValue != DBNull.Value ? (int)groupIdValue : null;
                            user.ScheduleManagement = (bool)reader["ScheduleManagement"];
                            user.UserManagement = (bool)reader["UserManagement"];
                            user.NewsManagement = (bool)reader["NewsManagement"];
                            user.BooksManagment = (bool)reader["BooksManagment"];
                        }
                    }
                }
            }
            return user;
        }
        public async Task<string?> GetGroupCodeByIdAsync(int? id)
        {
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("SELECT GroupCode FROM Groups WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    var groupCode = await command.ExecuteScalarAsync();

                    if (groupCode != null)
                    {
                        return groupCode.ToString();
                    }

                    return null;
                }
            }
        }
        public async Task<List<BookDTO>> GetBooksListAsync(int? groupId = null)
        {
            var books = new List<BookDTO>();

            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                string query = @$"
    SELECT Books.Id, Books.Title, Books.Author, Books.Cover FROM Books
JOIN GroupBooks ON Books.Id = GroupBooks.Bookid
JOIN Groups ON GroupBooks.GroupId = Groups.Id
WHERE Groups.Id = {groupId}
";
                if (groupId == null)
                {
                     query = "SELECT [Id],[Title],[Author],[Cover] FROM Books";
                }

                using (var command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var book = new BookDTO();
                            book.Id = int.Parse(reader["Id"].ToString());
                            book.Title = reader["Title"].ToString();
                            book.Author = reader["Author"].ToString();
                            book.Cover = reader["Cover"].ToString();
                            books.Add(book);
                        }
                    }
                }
            }
            return books;
        }
        public async Task<byte[]?> GetBookBytesByIdAsync(int id)
        {
            using (var connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("SELECT PDF FROM Books WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    var res = await command.ExecuteScalarAsync();

                    if (res != null)
                    {
                        return (byte[])res;
                    }

                    return null;
                }
            }
        }
        public async Task<List<GroupDTO>> GetGroupsListAsync()
        {
            var groups = new List<GroupDTO>();
            
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();

                string query = "SELECT Id, GroupCode FROM Groups";
                using (SqlCommand command = new SqlCommand(query,connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var group = new GroupDTO();
                            group.Id = int.Parse(reader["Id"].ToString());
                            group.GroupCode = reader["GroupCode"].ToString();
                            groups.Add(group);
                        }
                    }
                }
            }
            return groups;
        }
        public void AddBook(BookDTO dto, byte[] PDFbytes, List<int> ids)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string query = @"
INSERT INTO Books (Title, Author, Cover) VALUES (@Title, @Author, @Cover)
DECLARE @book_id INT;
SET @book_id = SCOPE_IDENTITY();
UPDATE Books SET PDF = @PDF WHERE Id = @book_id;
INSERT INTO GroupBooks (Bookid, GroupId) VALUES ";

                for (int i = 0; i < ids.Count(); i++)
                {
                    query += $"(@book_id,{ids[i]})";
                    if (i != ids.Count()-1)
                    {
                        query += ",";
                    }
                }

                using (SqlCommand command = new SqlCommand(query,connection))
                {
                    command.Parameters.AddWithValue("@Title", dto.Title);
                    command.Parameters.AddWithValue("@Author", dto.Author);
                    command.Parameters.AddWithValue("@PDF", PDFbytes);
                    command.Parameters.AddWithValue("@Cover", dto.Cover);

                    command.ExecuteNonQuery();

                }
            }
        }
        public void DeleteBookById(int id)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string query = "DELETE FROM GroupBooks WHERE Bookid = @id; DELETE FROM Books WHERE Id = @id;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    command.ExecuteNonQuery();
                }


            }
        }
        public async Task<BookDTO> GetBookByIdAsync(int id)
        {
            BookDTO book = new BookDTO();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                string query = "SELECT [Id],[Title],[Author],[Cover] FROM Books WHERE Id = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            book.Id = int.Parse(reader["Id"].ToString());
                            book.Title = reader["Title"].ToString();
                            book.Author = reader["Author"].ToString();
                            book.Cover = reader["Cover"].ToString();
                        }
                    }
                }
            }
            return book;
        }
        public void UpdateBook(BookDTO dto)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string query = @"
                    UPDATE Books
                    SET Title = @title, Author = @author, Cover = @cover
                    WHERE Id = @id
                ";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@title",dto.Title);
                    command.Parameters.AddWithValue("@author", dto.Author);
                    command.Parameters.AddWithValue("@cover", dto.Cover);
                    command.Parameters.AddWithValue("@id", dto.Id);

                    command.ExecuteNonQuery();
                }


            }
        }
        public void UpdateProfilePicById(int id, string base64)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string query = "UPDATE Users SET ProfilePic = @pic WHERE Id = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@pic", base64);
                    command.Parameters.AddWithValue("@id", id);

                    command.ExecuteNonQuery();
                }
            }
        }
        public async Task<byte[]?> GetScheduleByIdAsync(int groupId)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                string query = "SELECT ExcelDoc From Schedule WHERE GroupId = @groupId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupId", groupId);

                    var res = await command.ExecuteScalarAsync();
                    return (byte[])res;
                }
            }
            
        }
        public void AddScheduleById(ScheduleDTO dto)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                string query = @"
IF EXISTS (SELECT 1 FROM Schedule WHERE GroupId = @groupId)
    UPDATE Schedule SET ExcelDoc = @excelBytes WHERE GroupId = @groupId
ELSE
    INSERT INTO Schedule (GroupId, ExcelDoc) VALUES (@groupId, @excelBytes)

";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupId", dto.GroupId);
                    command.Parameters.AddWithValue("@excelBytes", dto.ExcelDoc);

                    command.ExecuteNonQuery();
                }
            }

        }
        public async Task<List<NewsDTO>> GetNewsAsync()
        {
            List<NewsDTO> news = new List<NewsDTO>();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                string query = "SELECT Id, Title, Body FROM News";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            NewsDTO card = new NewsDTO();
                            card.Id = int.Parse(reader["Id"].ToString());
                            card.Title = reader["Title"].ToString();
                            card.Body = reader["Body"].ToString();
                            news.Add(card);
                        }
                    }
                }
            }
            return news;
        }
        public void AddNews(NewsDTO dto)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                string query = "INSERT INTO News (Title, Body) VALUES (@title, @body)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@title", dto.Title);
                    command.Parameters.AddWithValue("@body", dto.Body);

                    command.ExecuteNonQuery();
                }
            }
        }
        public void DeleteNewsById(int id)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                string query = "DELETE FROM News WHERE Id = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    command.ExecuteNonQuery();
                }
            }
        }
        public void ChangePasswordById(int id, string password)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                string query = "UPDATE Users SET PasswordHash = @password WHERE id = @id";
                using (SqlCommand command = new SqlCommand(query,connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@password", Helpers.GeneratePasswordHash(password));

                    command.ExecuteNonQuery();
                }
            }
        }
        public async Task<List<UserDTO>> GetUsersBasicInfoAsync()
        {
            List<UserDTO> list = new List<UserDTO>();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                string query = "SELECT Id, Email, FirstName, Surname, Patronymic, GroupId FROM Users";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            UserDTO dto = new UserDTO();
                            dto.Id = int.Parse(reader["Id"].ToString());
                            dto.Email = reader["Email"].ToString();
                            dto.FirstName = reader["FirstName"].ToString();
                            dto.Surname = reader["Surname"].ToString();
                            dto.Patronymic = reader["Patronymic"].ToString();
                            var tmpGroupid = reader["GroupId"].ToString();
                            if (String.IsNullOrEmpty(tmpGroupid))
                            {
                                dto.GroupId = null;
                            }
                            else
                            {
                                dto.GroupId = int.Parse(tmpGroupid);
                            }
                            list.Add(dto);
                        }
                    }
                }
            }
            return list;
        }
        public async Task<List<RoleDTO>> GetRolesListAsync()
        {
            List<RoleDTO> list = new List<RoleDTO>();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                string query = "SELECT Id, RoleName FROM Roles";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            RoleDTO role = new RoleDTO();
                            role.Id = int.Parse(reader["Id"].ToString());
                            role.RoleName = reader["RoleName"].ToString();
                            list.Add(role);
                        }
                    }
                }
            }
            return list;
        }
        public void AddUser(UserDTO dto)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                string query = @"
    INSERT INTO Users (Email, PasswordHash, FirstName, Surname, Patronymic, ProfilePic, GroupId)
VALUES (@email, @passhash, @fname, @sname, @patr, @pfp, @groupId)
DECLARE @user_id INT;
SET @user_id = SCOPE_IDENTITY();
INSERT INTO UserRoles (UserId, RoleId) 
VALUES (@user_id, @roleId)
";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", dto.Email);
                    command.Parameters.AddWithValue("@passhash",dto.PasswordHash);
                    command.Parameters.AddWithValue("@fname",dto.FirstName);
                    command.Parameters.AddWithValue("@sname", dto.Surname);
                    command.Parameters.AddWithValue("@patr", dto.Patronymic);
                    command.Parameters.AddWithValue("@pfp", dto.ProfilePic);
                    command.Parameters.AddWithValue("@groupId", dto.GroupId);
                    command.Parameters.AddWithValue("@roleId", dto.RoleId);

                    command.ExecuteNonQuery();

                }
            }
        }
        public void DeleteUserById(int id)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                string query = "DELETE FROM UserRoles WHERE UserId = @id; DELETE FROM Users WHERE Id = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }
        public async Task<RoleDTO> GetRoleByUserIdAsync(int userId)
        {
            RoleDTO dto = new RoleDTO();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                string query = "SELECT Id, RoleName FROM Roles WHERE Id = (SELECT RoleId FROM UserRoles WHERE UserId = @userId)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dto.Id =  int.Parse(reader["Id"].ToString());
                            dto.RoleName = reader["RoleName"].ToString();
                        }
                    }
                }
            }
            return dto;
        }
        public async Task<UserDTO> GetUserByIdAsync(int id)
        {
            UserDTO dto = new UserDTO();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                await connection.OpenAsync();
                string query = @"
SELECT Users.Id as UserId, Email, PasswordHash, FirstName, Surname, Patronymic, ProfilePic, GroupId, Roles.Id as RoleId FROM Users 
JOIN UserRoles ON Users.Id = UserRoles.UserId
JOIN Roles ON UserRoles.RoleId = Roles.Id WHERE UserId = @id
";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dto.Id = int.Parse(reader["UserId"].ToString());
                            dto.Email = reader["Email"].ToString();
                            dto.PasswordHash = reader["PasswordHash"].ToString();
                            dto.FirstName = reader["FirstName"].ToString();
                            dto.Surname = reader["Surname"].ToString();
                            dto.Patronymic = reader["Patronymic"].ToString();
                            dto.ProfilePic = reader["ProfilePic"].ToString();
                            var tmp = reader["GroupId"].ToString();
                            if (string.IsNullOrEmpty(tmp))
                                dto.GroupId = null;
                            else
                                dto.GroupId = int.Parse(tmp);
                            dto.RoleId = int.Parse(reader["RoleId"].ToString());
                        }
                    }
                }
            }
            return dto;
        }
        public void UpdateUser(UserDTO dto)
        {
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                string query = @"UPDATE Users SET Email = @email, PasswordHash = @hash, FirstName = @fname, Surname = @sname, Patronymic = @patr, ProfilePic = @pfp, GroupId = @groupId WHERE Id = @id
                    UPDATE UserRoles SET RoleId = @roleId WHERE UserId = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", dto.Email);
                    command.Parameters.AddWithValue("@hash", dto.PasswordHash);
                    command.Parameters.AddWithValue("@fname", dto.FirstName);
                    command.Parameters.AddWithValue("@sname", dto.Surname);
                    command.Parameters.AddWithValue("@patr", dto.Patronymic);
                    command.Parameters.AddWithValue("@pfp", dto.ProfilePic);
                    command.Parameters.AddWithValue("@groupId", dto.GroupId);
                    command.Parameters.AddWithValue("@id", dto.Id);
                    command.Parameters.AddWithValue("@roleId", dto.RoleId);

                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
