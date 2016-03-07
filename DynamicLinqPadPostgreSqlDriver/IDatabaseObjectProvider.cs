using System.Reflection.Emit;
using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver
{
    public interface IDatabaseObjectProvider
    {
        ExplorerItem EmitCodeAndGetExplorerItemTree(TypeBuilder dataContextTypeBuilder);
    }
}