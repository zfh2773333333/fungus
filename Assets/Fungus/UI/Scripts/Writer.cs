﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Fungus
{

	public class Writer : MonoBehaviour 
	{
		public float writingSpeed = 60;
		public float punctuationPause = 0.25f;
		public Color hiddenTextColor = new Color(1,1,1,0);
		public bool writeWholeWords = false;
		
		protected float currentWritingSpeed;
		protected float currentPunctuationPause;
		protected Text textUI;
		protected InputField inputField;
		protected TextMesh textMesh;
		protected bool boldActive = false;
		protected bool italicActive = false;
		protected bool colorActive = false;
		protected string colorText = "";
		protected bool inputFlag;
		
		public string text 
		{
			get 
			{
				if (textUI != null)
				{
					return textUI.text;
				}
				else if (inputField != null)
				{
					return inputField.text;
				}
				else if (textMesh != null)
				{
					return textMesh.text;
				}
				return "";
			}
			
			set 
			{
				if (textUI != null)
				{
					textUI.text = value;
				}
				else if (inputField != null)
				{
					inputField.text = value;
				}
				else if (textMesh != null)
				{
					textMesh.text = value;
				}
			}
		}
		
		protected virtual void Awake()
		{
			textUI = GetComponent<Text>();
			inputField = GetComponent<InputField>();
			textMesh = GetComponent<TextMesh>();
		}
		
		public virtual bool HasTextObject()
		{
			return (textUI != null || inputField != null || textMesh != null);
		}
		
		public virtual bool SupportsRichText()
		{
			if (textUI != null)
			{
				return textUI.supportRichText;
			}
			if (inputField != null)
			{
				return false;
			}
			if (textMesh != null)
			{
				return textMesh.richText;
			}
			return false;
		}
		
		protected virtual string OpenMarkup()
		{
			string tagText = "";
			
			if (SupportsRichText())
			{
				if (colorActive)
				{
					tagText += "<color=" + colorText + ">"; 
				}
				if (boldActive)
				{
					tagText += "<b>"; 
				}
				if (italicActive)
				{
					tagText += "<i>"; 
				}			
			}
			
			return tagText;
		}
		
		protected virtual string CloseMarkup()
		{
			string closeText = "";
			
			if (SupportsRichText())
			{
				if (italicActive)
				{
					closeText += "</i>"; 
				}			
				if (boldActive)
				{
					closeText += "</b>"; 
				}
				if (colorActive)
				{
					closeText += "</color>"; 
				}
			}
			
			return closeText;		
		}
		
		protected virtual void Update()
		{
			if (Input.anyKeyDown)
			{
				SetInputFlag();
			}
		}
		
		public virtual void SetTextColor(Color textColor)
		{
			if (textUI != null)
			{
				textUI.color = textColor;
			}
			else if (inputField != null)
			{
				if (inputField.textComponent != null)
				{
					inputField.textComponent.color = textColor;
				}
			}
			else if (textMesh != null)
			{
				textMesh.color = textColor;
			}
		}
		
		public virtual void SetTextAlpha(float textAlpha)
		{
			if (textUI != null)
			{
				Color tempColor = textUI.color;
				tempColor.a = textAlpha;
				textUI.color = tempColor;
			}
			else if (inputField != null)
			{
				if (inputField.textComponent != null)
				{
					Color tempColor = inputField.textComponent.color;
					tempColor.a = textAlpha;
					inputField.textComponent.color = tempColor;
				}
			}
			else if (textMesh != null)
			{
				Color tempColor = textMesh.color;
				tempColor.a = textAlpha;
				textMesh.color = tempColor;
			}
		}
		
		public virtual void Write(string content, bool clear, Action onComplete = null)
		{
			if (clear)
			{
				this.text = "";
			}
			
			if (!HasTextObject())
			{
				return;
			}
			
			TextTagParser tagParser = new TextTagParser();
			List<TextTagParser.Token> tokens = tagParser.Tokenize(content);
			
			StartCoroutine(ProcessTokens(tokens, onComplete));
		}
		
		protected virtual IEnumerator ProcessTokens(List<TextTagParser.Token> tokens, Action onComplete)
		{
			text = "";
			
			// Reset control members
			boldActive = false;
			italicActive = false;
			colorActive = false;
			colorText = "";
			currentPunctuationPause = punctuationPause;
			currentWritingSpeed = writingSpeed;
			
			foreach (TextTagParser.Token token in tokens)
			{
				switch (token.type)
				{
				case TextTagParser.TokenType.Words:
					yield return StartCoroutine(DoWords(token.param));
					break;
					
				case TextTagParser.TokenType.BoldStart:
					boldActive = true;
					break;
					
				case TextTagParser.TokenType.BoldEnd:
					boldActive = false;
					break;
					
				case TextTagParser.TokenType.ItalicStart:
					italicActive = true;
					break;
					
				case TextTagParser.TokenType.ItalicEnd:
					italicActive = false;
					break;
					
				case TextTagParser.TokenType.ColorStart:
					colorActive = true;
					colorText = token.param;
					break;
					
				case TextTagParser.TokenType.ColorEnd:
					colorActive = false;
					break;
					
				case TextTagParser.TokenType.Wait:
					yield return StartCoroutine(DoWait(token.param));
					break;
					
				case TextTagParser.TokenType.WaitForInputNoClear:
					yield return StartCoroutine(DoWaitForInput(false));
					break;
					
				case TextTagParser.TokenType.WaitForInputAndClear:
					yield return StartCoroutine(DoWaitForInput(true));
					break;
					
				case TextTagParser.TokenType.WaitOnPunctuationStart:
					if (!Single.TryParse(token.param, out currentPunctuationPause))
					{
						currentPunctuationPause = punctuationPause;
					}
					break;
					
				case TextTagParser.TokenType.WaitOnPunctuationEnd:
					currentPunctuationPause = punctuationPause;
					break;
					
				case TextTagParser.TokenType.Clear:
					text = "";
					break;
					
				case TextTagParser.TokenType.SpeedStart:
					if (!Single.TryParse(token.param, out currentWritingSpeed))
					{
						currentWritingSpeed = writingSpeed;
					}
					break;
					
				case TextTagParser.TokenType.SpeedEnd:
					currentWritingSpeed = writingSpeed;
					break;
					
				case TextTagParser.TokenType.Exit:
					yield break;
					
				case TextTagParser.TokenType.Message:
					Flowchart.BroadcastFungusMessage(token.param);
					break;
					
				case TextTagParser.TokenType.VerticalPunch:
					float vintensity;
					if (!Single.TryParse(token.param, out vintensity))
					{
						vintensity = 10f;
					}
					Punch(new Vector3(0, vintensity, 0), 0.5f);
					break;
					
				case TextTagParser.TokenType.HorizontalPunch:
					float hintensity;
					if (!Single.TryParse(token.param, out hintensity))
					{
						hintensity = 10f;
					}
					Punch(new Vector3(hintensity, 0, 0), 0.5f);
					break;
					
				case TextTagParser.TokenType.Punch:
					float intensity;
					if (!Single.TryParse(token.param, out intensity))
					{
						intensity = 10f;
					}
					Punch(new Vector3(intensity, intensity, 0), 0.5f);
					break;
					
				case TextTagParser.TokenType.Flash:
					float flashDuration;
					if (!Single.TryParse(token.param, out flashDuration))
					{
						flashDuration = 0.2f;
					}
					Flash(flashDuration);
					break;
					
				case TextTagParser.TokenType.Audio:
				{
					AudioSource audioSource = FindAudio(token.param);
					if (audioSource != null)
					{
						audioSource.PlayOneShot(audioSource.clip);
					}
				}
					break;
					
				case TextTagParser.TokenType.AudioLoop:
				{
					AudioSource audioSource = FindAudio(token.param);
					if (audioSource != null)
					{
						audioSource.Play();
						audioSource.loop = true;
					}
				}
					break;
					
				case TextTagParser.TokenType.AudioPause:
				{
					AudioSource audioSource = FindAudio(token.param);
					if (audioSource != null)
					{
						audioSource.Pause();
					}
				}
					break;
					
				case TextTagParser.TokenType.AudioStop:
				{
					AudioSource audioSource = FindAudio(token.param);
					if (audioSource != null)
					{
						audioSource.Stop();
					}
				}
					break;
					
				}
				
				inputFlag = false;
			}
			
			if (onComplete != null)
			{
				onComplete();
			}
		}
		
		protected virtual IEnumerator DoWords(string param)
		{
			string startText = text;
			string openText = OpenMarkup();
			string closeText = CloseMarkup();
			
			float timeAccumulator = Time.deltaTime;

			for (int i = 0; i < param.Length; ++i)
			{
				string left = "";
				string right = "";
				
				PartitionString(writeWholeWords, param, i, out left, out right);
				text = ConcatenateString(startText, openText, closeText, left, right);

				// Punctuation pause
				if (left.Length > 0 && 
				    IsPunctuation(left.Substring(left.Length - 1)[0]))
				{
					yield return new WaitForSeconds(currentPunctuationPause);
				}

				// Delay between characters
				if (currentWritingSpeed > 0f)
				{
					if (timeAccumulator > 0f)
					{
						timeAccumulator -= 1f / currentWritingSpeed;
					}
					else
					{
						yield return new WaitForSeconds(1f / currentWritingSpeed);
					}
				}
			}
		}

		protected void PartitionString(bool wholeWords, string inputString, int i, out string left, out string right)
		{
			left = "";
			right = "";

			if (wholeWords)
			{
				// Look ahead to find next whitespace or end of string
				for (int j = i; j < inputString.Length; ++j)
				{
					if (Char.IsWhiteSpace(inputString[j]) ||
					    j == inputString.Length - 1)
					{
						left = inputString.Substring(0, j + 1);
						right = inputString.Substring(j + 1, inputString.Length - j - 1);
						break;
					}
				}
			}
			else
			{
				left = inputString.Substring(0, i + 1);
				right = inputString.Substring(i + 1);
			}
		}

		protected string ConcatenateString(string startText, string openText, string closeText, string leftText, string rightText)
		{
			string tempText = startText + openText + leftText + closeText;
		
			Color32 c = hiddenTextColor;
			string hiddenColor = String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", c.r, c.g, c.b, c.a);

			// Make right hand side text hidden
			if (rightText.Length > 0)
			{
				tempText += "<color=" + hiddenColor + ">" + rightText + "</color>";
			}
			
			return tempText;
		}

		public virtual void SetInputFlag()
		{
			inputFlag = true;
		}
		
		public virtual string GetTagHelp()
		{
			return "";
		}
		
		protected virtual IEnumerator DoWait(string param)
		{
			float duration = 1f;
			if (!Single.TryParse(param, out duration))
			{
				duration = 1f;
			}
			
			yield return new WaitForSeconds(duration);
		}
		
		protected virtual IEnumerator DoWaitForInput(bool clear)
		{
			while (!inputFlag)
			{
				yield return null;
			}
			
			inputFlag = false;
			
			if (clear)
			{
				textUI.text = "";
			}
		}
		
		protected virtual bool IsPunctuation(char character)
		{
			return character == '.' || 
				character == '?' ||  
					character == '!' || 
					character == ',' ||
					character == ':' ||
					character == ';' ||
					character == ')';
		}
		
		protected virtual void Punch(Vector3 axis, float time)
		{
			iTween.ShakePosition(this.gameObject, axis, time);
		}
		
		protected virtual void Flash(float duration)
		{
			CameraController cameraController = CameraController.GetInstance();
			cameraController.screenFadeTexture = CameraController.CreateColorTexture(new Color(1f,1f,1f,1f), 32, 32);
			cameraController.Fade(1f, duration, delegate {
				cameraController.screenFadeTexture = CameraController.CreateColorTexture(new Color(1f,1f,1f,1f), 32, 32);
				cameraController.Fade(0f, duration, null);
			});
		}
		
		protected virtual AudioSource FindAudio(string audioObjectName)
		{
			GameObject go = GameObject.Find(audioObjectName);
			if (go == null)
			{
				return null;
			}
			
			return go.GetComponent<AudioSource>();
		}
	}

}