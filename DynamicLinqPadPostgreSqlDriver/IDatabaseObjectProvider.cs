using System.Reflection.Emit;
using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver
{
    public interface IDatabaseObjectProvider
    {
        int Priority { get; }

        ExplorerItem EmitCodeAndGetExplorerItemTree(TypeBuilder dataContextTypeBuilder);
    }
}