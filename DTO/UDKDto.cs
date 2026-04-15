namespace biblioteka.DTO
{
    public class UDKDto
    {
        public int Id { get; }
        public string Code { get; }
        public string Description { get; }

        public UDKDto(int id, string code, string description)
        {
            Id = id;
            Code = code;
            Description = description;
        }
    }
}