using AutoMapper;

namespace OpenCqrs.Mapping
{
    public class ObjectFactory : IObjectFactory
    {
        private readonly IMapper _mapper;

        public ObjectFactory(IMapper mapper)
        {
            _mapper = mapper;
        }

        /// <inheritdoc />
        public dynamic CreateConcreteObject(object obj)
        {
            var type = obj.GetType();
            dynamic result = _mapper.Map(obj, type, type);
            return result;
        }
    }
}