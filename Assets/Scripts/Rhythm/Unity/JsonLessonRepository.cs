using System;
using UnityEngine;

namespace XRMidi
{
    public sealed class JsonLessonRepository : ILessonRepository
    {
        private readonly string json;
        public JsonLessonRepository(string json) { this.json = json; }
        public Lesson Load()
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Lesson JSON is empty.");
            var lesson = JsonUtility.FromJson<Lesson>(json);
            if (lesson == null) throw new ArgumentException("Lesson JSON has no lesson object.");
            lesson.Validate(); return lesson;
        }
    }
}
