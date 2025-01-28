using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.LoggerSystem;
using Newtonsoft.Json;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     Manages the narrative design for a ConvaiNPC.
    /// </summary>
    [RequireComponent(typeof(ConvaiNPC))]
    public class NarrativeDesignManager : MonoBehaviour
    {
        public List<SectionChangeEventsData> sectionChangeEventsDataList = new();
        public List<SectionData> sectionDataList = new();
        public List<TriggerData> triggerDataList = new();
        private ConvaiNPC _convaiNpc;
        private string _currentSectionID;
        private NarrativeDesignAPI _narrativeDesignAPI;

        private NarrativeDesignAPI NarrativeDesignAPI => _narrativeDesignAPI ??= new NarrativeDesignAPI();
        private ConvaiNPC ConvaiNpc => _convaiNpc ??= GetComponent<ConvaiNPC>();
        private string CharacterID => ConvaiNpc.characterID;
        
        private void Awake()
        {
            _convaiNpc = GetComponent<ConvaiNPC>();
            StartCoroutine(UpdateDataListsCoroutine());
        }
        private void Reset()
        {
            StartCoroutine(UpdateDataListsCoroutine());
        }
        private void ConvaiWebGLCommunicationHandler_OnNPCBTResponseReceived(string narrativeSectionId)
        {
            Debug.Log("NarrativeDesignManager: ConvaiWebGLCommunicationHandler_OnNPCBTResponseReceived: " + narrativeSectionId);
            UpdateCurrentSection(narrativeSectionId);
        }
        private IEnumerator UpdateDataListsCoroutine()
        {
            yield return StartCoroutine(UpdateSectionListCoroutine());
            yield return StartCoroutine(UpdateTriggerListCoroutine());
        }

        /// <summary>
        ///     Updates the section list from the server.
        /// </summary>
        public IEnumerator UpdateSectionListCoroutine()
        {
            yield return StartCoroutine(GetSectionListFromServerCoroutine());
        }

        /// <summary>
        ///     Updates the trigger list from the server.
        /// </summary>
        public IEnumerator UpdateTriggerListCoroutine()
        {
            yield return StartCoroutine(ListTriggersCoroutine(CharacterID));
        }

        /// <summary>
        ///     Invoked when the section event list changes.
        /// </summary>
        public void OnSectionEventListChange()
        {
            foreach (SectionChangeEventsData sectionChangeEventsData in sectionChangeEventsDataList)
                sectionChangeEventsData.Initialize(this);
        }

        private IEnumerator GetSectionListFromServerCoroutine()
        {
            yield return StartCoroutine(NarrativeDesignAPI.ListSectionsCoroutine(CharacterID, sections =>
            {
                if (sections != null)
                {
                    List<SectionData> updatedSectionList = JsonConvert.DeserializeObject<List<SectionData>>(sections);
                    UpdateSectionDataList(updatedSectionList);
                }
                else
                {
                    ConvaiLogger.Error("Failed to fetch section list", ConvaiLogger.LogCategory.Character);
                }
            }));
        }

        public event System.Action OnTriggersUpdated;

        private IEnumerator ListTriggersCoroutine(string characterId)
        {
            yield return StartCoroutine(NarrativeDesignAPI.GetTriggerListCoroutine(characterId, triggers =>
            {
                if (triggers != null)
                {
                    triggerDataList = JsonConvert.DeserializeObject<List<TriggerData>>(triggers);
                    OnTriggersUpdated?.Invoke();
                }
                else
                {
                    ConvaiLogger.Error("Failed to fetch trigger list", ConvaiLogger.LogCategory.Character);
                }
            }));
        }

        /// <summary>
        ///     Updates the current section.
        /// </summary>
        /// <param name="sectionID"> The section ID to update to. </param>
        public void UpdateCurrentSection(string sectionID)
        {
            if (string.IsNullOrEmpty(_currentSectionID))
            {
                _currentSectionID = sectionID;
                InvokeSectionEvent(_currentSectionID, true);
                return;
            }

            if (_currentSectionID.Equals(sectionID))
                return;

            InvokeSectionEvent(_currentSectionID, false);
            _currentSectionID = sectionID;
            InvokeSectionEvent(_currentSectionID, true);
        }

        private void InvokeSectionEvent(string id, bool isStarting)
        {
            SectionChangeEventsData sectionChangeEventsData = sectionChangeEventsDataList.Find(x => x.id == id);

            if (sectionChangeEventsData == null)
            {
                ConvaiLogger.Info($"No Section Change Events have been created for sectionID: {id}", ConvaiLogger.LogCategory.Actions);
                return;
            }

            if (isStarting)
                sectionChangeEventsData.onSectionStart?.Invoke();
            else
                sectionChangeEventsData.onSectionEnd?.Invoke();
        }

        private void UpdateSectionDataList(List<SectionData> updatedSectionList)
        {
            Dictionary<string, SectionData> updatedSectionDictionary = updatedSectionList.ToDictionary(s => s.sectionId);

            // Remove sections that no longer exist
            sectionDataList.RemoveAll(currentSection => !updatedSectionDictionary.ContainsKey(currentSection.sectionId));

            foreach (SectionData currentSection in sectionDataList.ToList())
            {
                if (updatedSectionDictionary.TryGetValue(currentSection.sectionId, out SectionData updatedSection))
                {
                    currentSection.sectionName = updatedSection.sectionName;
                    currentSection.objective = updatedSection.objective;
                    updatedSectionDictionary.Remove(currentSection.sectionId);
                }
            }

            foreach (SectionData newSection in updatedSectionDictionary.Values)
            {
                sectionDataList.Add(newSection);
            }

            foreach (SectionChangeEventsData sectionChangeEvent in sectionChangeEventsDataList)
            {
                sectionChangeEvent.Initialize(this);
            }
        }
    }
}
