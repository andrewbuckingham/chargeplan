    public interface IRepository<T>
    {
        Task<T> GetAsync(string id);
        Task<T> UploadAsync(string id, T entity);
        Task DeleteAsync(string id);
    }