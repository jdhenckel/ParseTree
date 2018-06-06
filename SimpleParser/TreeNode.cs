
namespace SimpleParser
{
    public class TreeNode
    {
        public string Name;
        public TreeNode Left;
        public TreeNode Right;

        public override string ToString()
        {
            if (Left == null && Right == null)
                return Name;
            if (Right == null)
                return Name + " (" + Left + ")";
            if (Left == null)
                return Name + " (" + Right + ")";
            return "(" + Left + " " + Name + " " + Right + ")";
        }
    }
}
