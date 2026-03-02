namespace PHPIL.Engine.SyntaxTree
{
    /// <summary>
    /// Represents a PHP <c>while</c> statement in the abstract syntax tree (AST).
    /// Encapsulates the loop condition and body block.
    /// </summary>
    /// <remarks>
    /// The <see cref="WhileNode"/> contains the conditional <see cref="Expression"/>
    /// that controls the loop and the <see cref="Body"/> block that executes repeatedly
    /// while the condition evaluates to true. Both members can be <c>null</c>
    /// in partially constructed or malformed ASTs.
    /// </remarks>
    public partial class WhileNode : SyntaxNode
    {
        /// <summary>
        /// The conditional expression of the <c>while</c> loop.
        /// Evaluated before each iteration to determine if the loop should continue.
        /// </summary>
        public ExpressionNode? Expression;
        
        /// <summary>
        /// The block of statements that form the body of the loop.
        /// Executed repeatedly as long as <see cref="Expression"/> evaluates to true.
        /// </summary>
        public BlockNode? Body;
    }
}