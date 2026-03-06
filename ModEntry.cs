using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace PortraitEditor
{
    public class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private bool IsEditMode = false;
        private string? CurrentNPC = null;
        private EditableProperty? SelectedProperty = null;
        private Rectangle EditButtonBounds;
        private const int EDIT_BUTTON_SIZE = 48;
        
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Load saved configs when game loads
            Config = Helper.ReadConfig<ModConfig>();
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is DialogueBox dialogueBox)
            {
                var npc = GetCurrentNPC(dialogueBox);
                if (npc != null)
                {
                    CurrentNPC = npc;
                    DrawEditButton(e.SpriteBatch);
                    
                    if (IsEditMode)
                    {
                        DrawEditUI(e.SpriteBatch, dialogueBox);
                    }
                }
            }
        }

        private void DrawEditButton(SpriteBatch b)
        {
            int screenWidth = Game1.uiViewport.Width;
            int screenHeight = Game1.uiViewport.Height;
            
            // Position button at top-right corner
            EditButtonBounds = new Rectangle(
                screenWidth - EDIT_BUTTON_SIZE - 20,
                20,
                EDIT_BUTTON_SIZE,
                EDIT_BUTTON_SIZE
            );
            
            // Draw button background
            Color buttonColor = IsEditMode ? Color.Yellow : Color.White;
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                EditButtonBounds.X,
                EditButtonBounds.Y,
                EditButtonBounds.Width,
                EditButtonBounds.Height,
                buttonColor,
                1f,
                false
            );
            
            // Draw icon (pencil/edit symbol)
            string text = IsEditMode ? "✓" : "✎";
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            b.DrawString(
                Game1.smallFont,
                text,
                new Vector2(
                    EditButtonBounds.X + (EditButtonBounds.Width - textSize.X) / 2,
                    EditButtonBounds.Y + (EditButtonBounds.Height - textSize.Y) / 2
                ),
                Color.Black
            );
        }

        private void DrawEditUI(SpriteBatch b, DialogueBox dialogueBox)
        {
            if (CurrentNPC == null) return;
            
            var npcConfig = GetOrCreateNPCConfig(CurrentNPC);
            int screenWidth = Game1.uiViewport.Width;
            int screenHeight = Game1.uiViewport.Height;
            
            // Draw semi-transparent overlay
            b.Draw(
                Game1.fadeToBlackRect,
                new Rectangle(0, 0, screenWidth, screenHeight),
                Color.Black * 0.3f
            );
            
            // Draw control panel
            int panelWidth = 400;
            int panelHeight = 500;
            int panelX = (screenWidth - panelWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 2;
            
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                panelX,
                panelY,
                panelWidth,
                panelHeight,
                Color.White,
                1f,
                false
            );
            
            // Draw title
            string title = $"Edit: {CurrentNPC}";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            b.DrawString(
                Game1.dialogueFont,
                title,
                new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 20),
                Color.Black
            );
            
            // Draw property controls
            int yOffset = panelY + 80;
            int lineHeight = 60;
            
            DrawPropertyControl(b, "Portrait Scale", npcConfig.PortraitScale, 0.1f, 3.0f, panelX + 20, yOffset, panelWidth - 40);
            yOffset += lineHeight;
            
            DrawPropertyControl(b, "Portrait X", npcConfig.PortraitOffsetX, -1000f, 1000f, panelX + 20, yOffset, panelWidth - 40);
            yOffset += lineHeight;
            
            DrawPropertyControl(b, "Portrait Y", npcConfig.PortraitOffsetY, -1000f, 1000f, panelX + 20, yOffset, panelWidth - 40);
            yOffset += lineHeight;
            
            DrawPropertyControl(b, "Dialogue Width", npcConfig.DialogueWidth, 100f, 1200f, panelX + 20, yOffset, panelWidth - 40);
            yOffset += lineHeight;
            
            DrawPropertyControl(b, "Dialogue Height", npcConfig.DialogueHeight, 100f, 800f, panelX + 20, yOffset, panelWidth - 40);
            yOffset += lineHeight;
            
            // Draw buttons
            yOffset += 20;
            DrawButton(b, "Save", panelX + 20, yOffset, 150, 50);
            DrawButton(b, "Reset", panelX + 190, yOffset, 150, 50);
            
            // Draw instructions
            string instructions = "Use arrow keys or click +/- to adjust values";
            Vector2 instructSize = Game1.smallFont.MeasureString(instructions);
            b.DrawString(
                Game1.smallFont,
                instructions,
                new Vector2(panelX + (panelWidth - instructSize.X) / 2, panelY + panelHeight - 40),
                Color.Gray
            );
        }

        private void DrawPropertyControl(SpriteBatch b, string label, float value, float min, float max, int x, int y, int width)
        {
            // Draw label
            b.DrawString(Game1.smallFont, label, new Vector2(x, y), Color.Black);
            
            // Draw value
            string valueText = value.ToString("F1");
            b.DrawString(Game1.smallFont, valueText, new Vector2(x + 200, y), Color.DarkBlue);
            
            // Draw +/- buttons
            DrawButton(b, "-", x + width - 100, y - 5, 40, 30);
            DrawButton(b, "+", x + width - 50, y - 5, 40, 30);
        }

        private void DrawButton(SpriteBatch b, string text, int x, int y, int width, int height)
        {
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                x, y, width, height,
                Color.White,
                1f,
                false
            );
            
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            b.DrawString(
                Game1.smallFont,
                text,
                new Vector2(
                    x + (width - textSize.X) / 2,
                    y + (height - textSize.Y) / 2
                ),
                Color.Black
            );
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is not DialogueBox) return;
            
            // Check if edit button was clicked
            if (e.Button == SButton.MouseLeft)
            {
                var mousePos = Game1.getMousePosition();
                if (EditButtonBounds.Contains(mousePos))
                {
                    IsEditMode = !IsEditMode;
                    Helper.Input.Suppress(e.Button);
                    return;
                }
            }
            
            // Handle edit mode controls
            if (IsEditMode && CurrentNPC != null)
            {
                HandleEditModeInput(e);
            }
        }

        private void HandleEditModeInput(ButtonPressedEventArgs e)
        {
            var npcConfig = GetOrCreateNPCConfig(CurrentNPC!);
            bool changed = false;
            float step = 10f;
            
            // Keyboard controls
            switch (e.Button)
            {
                case SButton.Up:
                    npcConfig.PortraitOffsetY -= step;
                    changed = true;
                    break;
                case SButton.Down:
                    npcConfig.PortraitOffsetY += step;
                    changed = true;
                    break;
                case SButton.Left:
                    npcConfig.PortraitOffsetX -= step;
                    changed = true;
                    break;
                case SButton.Right:
                    npcConfig.PortraitOffsetX += step;
                    changed = true;
                    break;
                case SButton.OemPlus:
                case SButton.Add:
                    npcConfig.PortraitScale += 0.1f;
                    changed = true;
                    break;
                case SButton.OemMinus:
                case SButton.Subtract:
                    npcConfig.PortraitScale = Math.Max(0.1f, npcConfig.PortraitScale - 0.1f);
                    changed = true;
                    break;
                case SButton.S:
                    SaveConfig();
                    Game1.addHUDMessage(new HUDMessage($"Saved settings for {CurrentNPC}", 1));
                    Helper.Input.Suppress(e.Button);
                    break;
                case SButton.R:
                    ResetNPCConfig(CurrentNPC!);
                    Game1.addHUDMessage(new HUDMessage($"Reset settings for {CurrentNPC}", 2));
                    Helper.Input.Suppress(e.Button);
                    break;
                case SButton.Escape:
                    IsEditMode = false;
                    Helper.Input.Suppress(e.Button);
                    break;
            }
            
            if (changed)
            {
                Helper.Input.Suppress(e.Button);
            }
        }

        private string? GetCurrentNPC(DialogueBox dialogueBox)
        {
            try
            {
                var speaker = Helper.Reflection.GetField<NPC>(dialogueBox, "speaker", false)?.GetValue();
                return speaker?.Name;
            }
            catch
            {
                return null;
            }
        }

        private NPCConfig GetOrCreateNPCConfig(string npcName)
        {
            if (!Config.NPCSettings.ContainsKey(npcName))
            {
                Config.NPCSettings[npcName] = new NPCConfig();
            }
            return Config.NPCSettings[npcName];
        }

        private void ResetNPCConfig(string npcName)
        {
            Config.NPCSettings[npcName] = new NPCConfig();
        }

        private void SaveConfig()
        {
            Helper.WriteConfig(Config);
        }
    }

    public class ModConfig
    {
        public Dictionary<string, NPCConfig> NPCSettings { get; set; } = new();
    }

    public class NPCConfig
    {
        public float PortraitScale { get; set; } = 1.0f;
        public float PortraitOffsetX { get; set; } = 0f;
        public float PortraitOffsetY { get; set; } = 0f;
        public float DialogueWidth { get; set; } = 900f;
        public float DialogueHeight { get; set; } = 384f;
    }

    public enum EditableProperty
    {
        PortraitScale,
        PortraitX,
        PortraitY,
        DialogueWidth,
        DialogueHeight
    }
}
