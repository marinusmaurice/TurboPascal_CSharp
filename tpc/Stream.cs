using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 

namespace tpc
{
    // Character streamer. Streams characters from the input (a string) one at a
    // time, including peeking. Returns -1 on end of file.
    internal class Stream
    {
        public string input;
        public int position;
        public int lineNumber;

        public Stream(string input)
        {
            this.input = input;
            this.position = 0;
            this.lineNumber = 1;
        }

        // Returns the next character, or -1 on end of file.
        public char next()
        {
            var ch = this.peek();
            if (ch == '\n')
            {
                this.lineNumber++;
            }
            if (ch != -1)
            {
                this.position++;
            }
            return ch;
        }

        // Peeks at the next character, or -1 on end of file.
        public char peek()
        {
            if (this.position >= this.input.Length)
            {
                return char.MaxValue;  //TODO: MVM
            }
            return this.input[this.position];
        }

        // Inverse of "next()" method.
        public void pushBack(char ch)
        {
            if (this.position == 0)
            {
                throw new Exception("Can't push back at start of stream");
            }
            this.position--;
            // Sanity check.
            if (this.input[this.position] != ch)
            {
                throw new Exception("Pushed back character doesn't match");
            }
        }

    }
}
