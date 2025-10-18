using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using ProgramX.Azure.FunctionApp.Constants;
using ProgramX.Azure.FunctionApp.Helpers;

namespace ProgramX.Azure.FunctionApp.Tests.Mocks;

public class InMemoryPagedReader<T> : IPagedReader<T>
{
    private List<T> _items = new List<T>();
    
    public InMemoryPagedReader()
    {
        SeedPagedReaderWithRandomData(100);
    }

    public InMemoryPagedReader(IEnumerable<T> items)
    {
        _items = items.ToList();    
    }
    
    private void SeedPagedReaderWithRandomData(int numberOfItemsRequired)
    {
        
        for (int i = 0; i < numberOfItemsRequired; i++)
        {
            _items.Add(CreateRandomItemForType<T>());        
        }
    }

    private TCreateForType CreateRandomItemForType<TCreateForType>()
    {
        var o =  Activator.CreateInstance(typeof(TCreateForType));
        if (o == null) throw new InvalidOperationException("Could not create instance of type");
        
        var fields = typeof(TCreateForType).GetFields();
        foreach (var field in fields)
        {
            SetFieldValue(o,field);
        }
        
        var properties = typeof(TCreateForType).GetProperties();
        foreach (var property in properties)
        {
            SetPropertyValue(o,property);       
        }
        
        return (TCreateForType)o;   
    }

    private void SetPropertyValue(object targetObject, PropertyInfo propertyInfo)
    {
        var memberType = propertyInfo.PropertyType;
        
        if (IsCollection(memberType))
        {
            if (memberType.IsGenericType)
            {
                Type typeOfGenericCollection = memberType.GetGenericArguments()[0];
                propertyInfo.SetValue(targetObject,Enumerable.Repeat(CreateRandomItemForType(typeOfGenericCollection), 1).ToList());    
            }
            else
            {
                propertyInfo.SetValue(targetObject,Enumerable.Repeat(CreateRandomItemForType(memberType.UnderlyingSystemType), 1).ToList());    
            }
            
        }
        else
        {
            if (propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(targetObject,CreateRandomItemForPrimitiveType(memberType));
            }
            else
            {
                // cannot set property value
                int i = 5;
            }
        }
    }

  

    private void SetFieldValue(object targetObject,FieldInfo fieldInfo)
    {
        var memberType = fieldInfo.FieldType;
        
        if (IsCollection(memberType))
        {
            if (memberType.IsGenericType)
            {
                Type typeOfGenericCollection = memberType.GetGenericArguments()[0];
                fieldInfo.SetValue(targetObject,Enumerable.Repeat(CreateRandomItemForType(typeOfGenericCollection), 1).ToList());    
            }
            else
            {
                fieldInfo.SetValue(targetObject,Enumerable.Repeat(CreateRandomItemForType(memberType.UnderlyingSystemType), 1).ToList());    
            }
        }
        else
        {
            fieldInfo.SetValue(targetObject,CreateRandomItemForPrimitiveType(memberType));
        }
    }

    private object? CreateRandomItemForPrimitiveType(Type memberType)
    {
        if (memberType == typeof(string))
        {
            return Guid.NewGuid().ToString();
        } else if (memberType == typeof(int))
        {
            return new Random().Next(1,100);
        } else if (memberType == typeof(bool))
        {
            return new Random().Next(0,1) == 1;
        } else if (memberType == typeof(DateTime))
        {
            return DateTime.Now;
        } else if (memberType == typeof(double))
        {
            return new Random().NextDouble();
        } else if (memberType == typeof(float))
            return new Random().NextDouble();
        else if (memberType == typeof(decimal))
            return new Random().NextDouble();
        else if (memberType == typeof(Guid))
            return Guid.NewGuid();
        else if (memberType == typeof(long))
            return new Random().Next();
        else if (memberType == typeof(byte))
        {
            var bytes = new byte[1];
            new Random().NextBytes(bytes);
            return bytes[0];
        }
        else if (memberType == typeof(short))
            return new Random().Next(short.MinValue,short.MaxValue);
        else if (memberType == typeof(uint))
            return new Random().Next((int)uint.MinValue,int.MaxValue);
        else if (memberType == typeof(ushort))
            return new Random().Next(ushort.MinValue,ushort.MaxValue);
        else if (memberType == typeof(ulong))
            return new Random().Next((int)ulong.MinValue,int.MaxValue);
        else if (memberType == typeof(sbyte))
            return new Random().Next(sbyte.MinValue,sbyte.MaxValue);
        else if (memberType == typeof(char))
            return new Random().Next(char.MinValue,char.MaxValue);
        else if (memberType == typeof(TimeSpan))
            return TimeSpan.FromSeconds(new Random().Next(1,100));
        else if (memberType == typeof(DateTimeOffset))
            return DateTimeOffset.Now;
        else if (memberType == typeof(byte[]))
            return new byte[10];
        else if (memberType.IsClass)
            return CreateRandomItemForType(memberType);
        else if (IsCollection(memberType))
            return Enumerable.Repeat(CreateRandomItemForType(memberType), 1).ToList();
        else if (memberType.Name == "Nullable`1")
        {
            var underlyingType = memberType.GenericTypeArguments[0];
            return CreateRandomItemForPrimitiveType(underlyingType);       
        }
        else
            throw new InvalidOperationException($"Could not create random item for type {memberType.Name}");
    }
    
    public static bool IsCollection(Type type)
    {
        // Handle null
        if (type == null) return false;
    
        // Exclude string explicitly
        if (type == typeof(string)) return false;
    
        // Check if it's an array
        if (type.IsArray) return true;
    
        // Check for common collection interfaces
        var interfaceNames = type.GetInterfaces().Select(i => i.Name);
        if (interfaceNames.Contains("IEnumerable`1")) return true;
        if (interfaceNames.Contains("ICollection`1")) return true;
        if (interfaceNames.Contains("IList`1")) return true;
        if (interfaceNames.Contains("IDictionary`2")) return true;
        if (interfaceNames.Contains("IReadOnlyCollection`1")) return true;
        if (interfaceNames.Contains("IReadOnlyList`1")) return true;
        if (interfaceNames.Contains("ISet`1")) return true;
        if (interfaceNames.Contains("IReadOnlySet`1")) return true;
        if (interfaceNames.Contains("ICollection")) return true;
        if (interfaceNames.Contains("IList")) return true;
        if (interfaceNames.Contains("IDictionary")) return true;
        if (interfaceNames.Contains("IReadOnlyCollection")) return true;
        if (interfaceNames.Contains("IReadOnlyList")) return true;
        if (interfaceNames.Contains("ISet")) return true;
        if (interfaceNames.Contains("IReadOnlySet")) return true;
        if (interfaceNames.Contains("IEnumerable")) return true;
        if (interfaceNames.Contains("IAsyncEnumerable`1")) return true;
        if (interfaceNames.Contains("IAsyncCollection`1")) return true;
        if (interfaceNames.Contains("IAsyncList`1")) return true;
        if (interfaceNames.Contains("IAsyncReadOnlyCollection`1")) return true;
        if (interfaceNames.Contains("IAsyncReadOnlyList`1")) return true;
        if (interfaceNames.Contains("IAsyncSet`1")) return true;
        if (interfaceNames.Contains("IAsyncReadOnlySet`1")) return true;
        if (interfaceNames.Contains("IAsyncEnumerable")) return true;

        return false;
    }
    

    public async Task<PagedCosmosDbResult<T>> GetNextItemsAsync(QueryDefinition queryDefinition, string? continuationToken = null, int? itemsPerPage = null)
    {
        return new PagedCosmosDbResult<T>(
            _items.Skip(0).Take(itemsPerPage ?? 100).ToList(),
            continuationToken,
            itemsPerPage,
            4.1,
            _items.Count,
            0);
    }

    public async Task<PagedCosmosDbResult<T>> GetPagedItemsAsync(QueryDefinition queryDefinition, string? orderByField, int? offset,
        int? itemsPerPage = DataConstants.ItemsPerPage)
    {
        return new PagedCosmosDbResult<T>(
            _items.Skip(offset ?? 0).Take(itemsPerPage ?? 100).ToList(),
            null,
            itemsPerPage,
            4.1,
            _items.Count,
            0);
    }
}