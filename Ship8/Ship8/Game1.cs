using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;

namespace Ship8
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        KeyboardState keyboard;
        SpriteBatch spriteBatch;
        SpriteFont mainFont;

        Texture2D pixel;                    // Textura do pixel
        Random random;                      // Gerador de Números Aleatorios

        byte[] mem = new byte[4096];        // Memória Principal
        int[] gfx = new int[64 * 32];       // Memoria Grafica
        byte[] v = new byte[16];            // Registradores

        bool draw = false;                  // Flag para redesenhar

        bool[] keys = new bool[16];         // Teclado

        ushort i;                           // Instruction Pointer
        ushort pc;                          // Program Counter
        ushort opcode;                      // Opcode atual

        Stack<ushort> stack;                // Stack

        byte delay_timer;                   // Timer geral
        byte sound_timer;

        // Programa a ser carregado
        byte[] program;

        // Fontset padrão do Chip-8
        byte[] chip8Font = new byte[80]
        { 
          0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
          0x20, 0x60, 0x20, 0x20, 0x70, // 1
          0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
          0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
          0x90, 0x90, 0xF0, 0x10, 0x10, // 4
          0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
          0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
          0xF0, 0x10, 0x20, 0x40, 0x40, // 7
          0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
          0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
          0xF0, 0x90, 0xF0, 0x90, 0x90, // A
          0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
          0xF0, 0x80, 0x80, 0x80, 0xF0, // C
          0xE0, 0x90, 0x90, 0x90, 0xE0, // D
          0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
          0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        String status = "";

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            
            // Carrega o programa
            program = File.ReadAllBytes("Content\\Roms\\INVADERS");
            random = new Random();
            stack = new Stack<ushort>();

            initVM();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            mainFont = Content.Load<SpriteFont>("Font/Main");
            pixel = Content.Load<Texture2D>("Textures/Pixel10");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            checkKeys();

            // Atualiza Timers

            if (delay_timer > 0)
            {
                delay_timer--;
            }

            if (sound_timer > 0)
            {
                if (sound_timer == 1)
                {
                    // BEEP
                }
                sound_timer--;

            }


            // TODO: Add your update logic here
            opcode = (ushort)(mem[pc] << 8 | mem[pc + 1]);

            switch (opcode & 0xF000)
            {
                case 0x0000:
                    switch (opcode & 0x000f)
                    {
                        // 00E0: Limpa a Tela
                        case 0x0000:
                            for (int p = 0; p < gfx.Length; p++)
                            {
                                gfx[p] = 0;
                            }
                            pc += 2;
                            break;

                        // 00EE: Retorna de uma Subrotina
                        case 0x000E:
                            pc = stack.Pop();
                            pc += 2;
                            break;

                        // Opcode não implementado
                        default:
                            Console.WriteLine("Opcode não implementado!");
                            break;
                    }
                    break;

                case 0x1000:
                    pc = (ushort)(opcode & 0x0FFF);
                    break;

                case 0x2000:
                    stack.Push(pc);
                    pc = (ushort)(opcode & 0x0FFF);
                    break;

                case 0x3000:
                    if (v[(opcode & 0x0F00) >> 8] == (opcode & 0x00FF))
                    {
                        pc += 4;
                    }
                    else
                    {
                        pc += 2;
                    }
                    break;

                case 0x4000:
                    if (v[(opcode & 0x0F00) >> 8] != (opcode & 0x00FF))
                    {
                        pc += 4;
                    }
                    else
                    {
                        pc += 2;
                    }
                    break;

                case 0x5000:
                    if (v[(opcode & 0x0F00) >> 8] == v[(opcode & 0x00F0) >> 4])
                    {
                        pc += 4;
                    }
                    else
                    {
                        pc += 2;
                    }
                    break;

                case 0x6000:
                    v[(opcode & 0x0F00) >> 8] = (byte)(opcode & 0x00FF);
                    pc += 2;
                    break;

                case 0x7000:
                    v[(opcode & 0x0F00) >> 8] += (byte)(opcode & 0x00FF);
                    pc += 2;
                    break;

                case 0x8000:
                    switch (opcode & 0x000F)
                    {
                        case 0x0000:
                            v[(opcode & 0x0F00) >> 8] = v[(opcode & 0x00F0) >> 4];
                            pc += 2;
                            break;

                        case 0x0001:
                            v[(opcode & 0x0F00) >> 8] |= v[(opcode & 0x00F0) >> 4];
                            pc += 2;
                            break;

                        case 0x0002:
                            v[(opcode & 0x0F00) >> 8] &= v[(opcode & 0x00F0) >> 4];
                            pc += 2;
                            break;

                        case 0x0003:
                            v[(opcode & 0x0F00) >> 8] ^= v[(opcode & 0x00F0) >> 4];
                            pc += 2;
                            break;

                        case 0x0004:
                            if ((v[(opcode & 0x0F00) >> 8] + v[(opcode & 0x00F0) >> 4]) > 10)
                            {
                                v[0xF] = 1;
                            }
                            else
                            {
                                v[0xF] = 0;
                            }
                            pc += 2;
                            break;

                        case 0x0005:
                            if (v[(opcode & 0x00F0) >> 4] > v[(opcode & 0x0F00) >> 8])
                            {
                                v[0xF] = 0;
                            }
                            else
                            {
                                v[0xF] = 1;
                            }
                            v[(opcode & 0x0F00) >> 8] -= v[(opcode & 0x00F0) >> 4];
                            pc += 2;
                            break;

                        case 0x0006:
                            v[0xF] = (byte)(v[(opcode & 0x0F00) >> 8] & 0x1);
                            v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] >> 1);
                            pc += 2;
                            break;

                        case 0x000E:
                            v[0xF] = (byte)((v[(opcode & 0x0F00) >> 8] & 0x80) >> 7);
                            v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] << 1);
                            pc += 2;
                            break;

                        default:
                            Console.WriteLine("Opcode não implementado!");
                            break;
                    }
                    break;

                case 0x9000:
                    if (v[(opcode & 0x0F00) >> 8] != v[(opcode & 0x00F0) >> 4])
                    {
                        pc += 4;
                    }
                    else
                    {
                        pc += 2;
                    }
                    break;

                case 0xA000:
                    i = (ushort)(opcode & 0x0FFF);
                    pc += 2;
                    break;

                case 0xC000:
                    v[(opcode & 0x0F00) >> 8] = (byte)(random.Next(0xFF) & (opcode & 0x00FF));
                    pc += 2;
                    break;

                case 0xD000:
                    ushort x = v[(opcode & 0x0F00) >> 8];
                    ushort y = v[(opcode & 0x00F0) >> 4];
                    ushort height = (ushort)(opcode & 0x000F);
                    ushort pixel;

                    v[0xF] = 0;

                    for (int _y = 0; _y < height; _y++)
                    {
                        pixel = mem[i + _y];

                        for (int _x = 0; _x < 8; _x++)
                        {
                            if((pixel & (0x80 >> _x)) != 0)
                            {
                                if (gfx[(x + _x + ((y + _y) * 64))] == 1)
                                {
                                    v[0xF] = 1;
                                }
                                gfx[x + _x + ((y + _y) * 64)] ^= 1;
                            }
                        }
                    }

                    draw = true;
                    pc += 2;
                    break;

                case 0xE000:
                    switch (opcode & 0x000F)
                    {
                        case 0x000E:
                            if (keys[v[(opcode & 0x0F00) >> 8]])
                            {
                                pc += 4;
                            }
                            else
                            {
                                pc += 2;
                            }
                            break;

                        case 0x0001:
                            if (!keys[v[(opcode & 0x0F00) >> 8]])
                            {
                                pc += 4;
                            }
                            else
                            {
                                pc += 2;
                            }
                            break;

                        default:
                            Console.WriteLine("Opcode não implementado!");
                            break;
                    }
                    break;

                case 0xF000:
                    switch (opcode & 0x00ff)
                    {
                        case 0x0007:
                            v[(opcode & 0x0F00) >> 8] = delay_timer;
                            pc += 2;
                            break;

                        case 0x000A:
                            for (int k = 0; k < keys.Length; k++)
                            {
                                if (keys[k])
                                {
                                    v[(opcode & 0x0F00) >> 8] = (byte)k;
                                    pc += 2;
                                    break;
                                }
                            }
                            break;

                        case 0x0015:
                            delay_timer = v[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;

                        case 0x0018:
                            sound_timer = v[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;

                        case 0x001E:
                            if (i + v[(opcode & 0x0F00) >> 8] > 0xFFF)
                            {
                                v[0xF] = 1;
                            }
                            else
                            {
                                v[0xF] = 0;
                            }
                            i += v[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;

                        case 0x0029:
                            i = (ushort)(v[(opcode & 0x0F00) >> 8] * 0x5);
                            pc += 2;
                            break;

                        case 0x0033:
                            mem[i] = (byte)(v[(opcode & 0x0F00) >> 8] / 100);
                            mem[i+1] = (byte)((v[(opcode & 0x0F00) >> 8] / 10) % 10);
                            mem[i+2] = (byte)((v[(opcode & 0x0F00) >> 8] % 100) % 10);
                            pc += 2;
                            break;

                        case 0x0055:
                            for (int j = 0; j <= ((opcode & 0x0F00) >> 8); j++)
                            {
                                mem[i + j] = v[j];
                            }
                            i += (ushort)(((opcode & 0x0F00) >> 8) + 1);
                            pc += 2;
                            break;

                        case 0x0065:
                            for (int j = 0; j <= ((opcode & 0x0F00) >> 8); j++)
                            {
                                v[j] = mem[i + j];
                            }
                            i += (ushort)(((opcode & 0x0F00) >> 8) + 1);
                            pc += 2;
                            break;

                        default:
                            Console.WriteLine("Opcode não implementado!");
                            break;
                    }
                    break;

                default:
                    Console.WriteLine("Opcode não implementado!");
                    break;
            }
            // Console.WriteLine("Opcode: " + opcode.ToString("X"));
            base.Update(gameTime);
        }

        private void checkKeys()
        {
            // CHIP-8:
            // 1 2 3 C
            // 4 5 6 D
            // 7 8 9 E
            // A 0 B F
            // 
            // PC:
            // 1 2 3 4
            // Q W E R
            // A S D F
            // Z X C V
            keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.X))
            {
                keys[0] = true;
            }
            else
            {
                keys[0] = false;
            }

            if (keyboard.IsKeyDown(Keys.D1))
            {
                keys[1] = true;
            }
            else
            {
                keys[1] = false;
            }

            if (keyboard.IsKeyDown(Keys.D2))
            {
                keys[2] = true;
            }
            else
            {
                keys[2] = false;
            }

            if (keyboard.IsKeyDown(Keys.D3))
            {
                keys[3] = true;
            }
            else
            {
                keys[3] = false;
            }

            if (keyboard.IsKeyDown(Keys.Q))
            {
                keys[4] = true;
            }
            else
            {
                keys[4] = false;
            }

            if (keyboard.IsKeyDown(Keys.W))
            {
                keys[5] = true;
            }
            else
            {
                keys[5] = false;
            }

            if (keyboard.IsKeyDown(Keys.E))
            {
                keys[6] = true;
            }
            else
            {
                keys[6] = false;
            }

            if (keyboard.IsKeyDown(Keys.A))
            {
                keys[7] = true;
            }
            else
            {
                keys[7] = false;
            }

            if (keyboard.IsKeyDown(Keys.S))
            {
                keys[8] = true;
            }
            else
            {
                keys[8] = false;
            }

            if (keyboard.IsKeyDown(Keys.D))
            {
                keys[9] = true;
            }
            else
            {
                keys[9] = false;
            }

            if (keyboard.IsKeyDown(Keys.Z))
            {
                keys[0xA] = true;
            }
            else
            {
                keys[0xA] = false;
            }

            if (keyboard.IsKeyDown(Keys.C))
            {
                keys[0xB] = true;
            }
            else
            {
                keys[0xB] = false;
            }

            if (keyboard.IsKeyDown(Keys.D4))
            {
                keys[0xC] = true;
            }
            else
            {
                keys[0xC] = false;
            }

            if (keyboard.IsKeyDown(Keys.R))
            {
                keys[0xD] = true;
            }
            else
            {
                keys[0xD] = false;
            }

            if (keyboard.IsKeyDown(Keys.F))
            {
                keys[0xE] = true;
            }
            else
            {
                keys[0xE] = false;
            }

            if (keyboard.IsKeyDown(Keys.V))
            {
                keys[0xF] = true;
            }
            else
            {
                keys[0xF] = false;
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(mainFont, dump(), new Vector2(640.0f, 0.0f), Color.White);
            drawGfxMemory();
            spriteBatch.End();
            base.Draw(gameTime);

        }

        private void initVM()
        {
            for (int x = 0; x < mem.Length; x++)
            {
                mem[x] = 0;
            }

            for (int x = 0; x < gfx.Length; x++)
            {
                gfx[x] = 0;
            }

            for (int x = 0; x < v.Length; x++)
            {
                v[x] = 0;
            }

            for (int f = 0; f < chip8Font.Length; f++)
            {
                mem[f] = chip8Font[f];
            }

            for (int x = 0; x < program.Length; x++)
            {
                // Carrega o programa na memória
                // a partir do endereço 0x200
                mem[512 + x] = program[x];
            }

            pc = 0x200;
            i = 0;
        }

        private String dump()
        {
            String dump = "";
            dump += "PC: " + pc.ToString("X") +"\n";
            dump += "OC: " + opcode.ToString("X") +"\n";
            dump += " I: " + i.ToString("X") +"\n";
            dump += "V0: " + v[0].ToString("X") +" V8: " + v[8].ToString("X") + "\n";
            dump += "V1: " + v[1].ToString("X") +" V9: " + v[9].ToString("X") + "\n";
            dump += "V2: " + v[2].ToString("X") +" VA: " + v[10].ToString("X") + "\n";
            dump += "V3: " + v[3].ToString("X") +" VB: " + v[11].ToString("X") + "\n";
            dump += "V4: " + v[4].ToString("X") +" VC: " + v[12].ToString("X") + "\n";
            dump += "V5: " + v[5].ToString("X") +" VD: " + v[13].ToString("X") + "\n";
            dump += "V6: " + v[6].ToString("X") +" VE: " + v[14].ToString("X") + "\n";
            dump += "V7: " + v[7].ToString("X") +" VF: " + v[15].ToString("X") + "\n";
            dump += status;
            for (int k = 0; k < 16; k++)
            {
                if (keys[k])
                {
                    dump += "Key " + k.ToString("X") + " pressed" + "\n";
                }
            }
            return dump;
        }

        private void drawGfxMemory()
        {
            for (int d = 0; d < gfx.Length; d++)
            {
                int _y = d / 64;
                int _x = d - (_y * 64);

                if (gfx[d] == 1)
                {
                    spriteBatch.Draw(pixel, new Vector2(_x*pixel.Width, _y*pixel.Height), Color.White);
                }
            }
        }
    }
}
