using System.Linq.Expressions;
using LeaveDesk.Application.Interfaces;
using Moq;

namespace LeaveDesk.Tests.TestHelpers;

public static class MockRepositoryExtensions
{
    public static void SetupInMemory<T>(this Mock<IRepository<T>> mock, List<T> items, Func<T, int> idSelector)
        where T : class
    {
        mock.Setup(r => r.GetAllAsync()).ReturnsAsync(() => items.ToList());

        mock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => items.FirstOrDefault(i => idSelector(i) == id));

        mock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<T, bool>>>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate) => items.FirstOrDefault(predicate.Compile()));

        mock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<T, bool>>>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate) => items.Any(predicate.Compile()));

        mock.Setup(r => r.AddAsync(It.IsAny<T>()))
            .Callback((T item) => items.Add(item))
            .Returns(Task.CompletedTask);

        mock.Setup(r => r.UpdateAsync(It.IsAny<T>()))
            .Returns(Task.CompletedTask);

        mock.Setup(r => r.DeleteAsync(It.IsAny<T>()))
            .Callback((T item) => items.Remove(item))
            .Returns(Task.CompletedTask);
    }
}
