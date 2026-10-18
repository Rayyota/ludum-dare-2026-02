using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tamagotchi.Tests
{
    public class ContactMatchingTests
    {
        [UnityTest]
        public IEnumerator SuppliedConnectorsReceiveDragsAndMatchTheirSockets()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Minigames/ContactMatching.unity");
            yield return new EnterPlayMode();
            yield return null;
            var game = Object.FindFirstObjectByType<ContactMatchingGame>();
            Assert.That(game, Is.Not.Null, "The contact matching manager is present.");
            var contacts = Field<ContactDrag[]>(game, "contacts");
            Assert.That(contacts.Length, Is.EqualTo(4));
            foreach (var contact in contacts)
            {
                Assert.That(contact, Is.Not.Null, "The contact reference is assigned.");
                var image = contact.GetComponent<Image>();
                Assert.That(image, Is.Not.Null);
                Assert.That(image.sprite, Is.Not.Null);
                Assert.That(image.rectTransform.rect.width / image.sprite.rect.width, Is.EqualTo(4));
                Assert.That(image.rectTransform.rect.height / image.sprite.rect.height, Is.EqualTo(4));
                var target = Field<RectTransform>(contact, "targetSnapPoint");
                var plug = Field<RectTransform>(contact, "plugSnapPoint");
                Assert.That(target, Is.Not.Null);
                Assert.That(plug, Is.Not.Null);
                Assert.That(EventSystem.current, Is.Not.Null, "The scene has an active EventSystem.");
                var pointer = new PointerEventData(EventSystem.current) {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, contact.transform.position)
                };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(hits[0].gameObject, Is.EqualTo(contact.gameObject));
                Vector2 start = contact.GetPosition();
                contact.OnBeginDrag(pointer);
                pointer.position += new Vector2(0, 15);
                contact.OnDrag(pointer);
                contact.OnEndDrag(pointer);
                Assert.That(contact.GetPosition(), Is.EqualTo(start), "Incorrect drops return to their slot.");
                pointer.position = RectTransformUtility.WorldToScreenPoint(null, contact.transform.position);
                contact.OnBeginDrag(pointer);
                pointer.position += RectTransformUtility.WorldToScreenPoint(null, target.position) -
                    RectTransformUtility.WorldToScreenPoint(null, plug.position);
                contact.OnDrag(pointer);
                int count = Field<int>(game, "connectedCount") + 1;
                LogAssert.Expect(LogType.Log, $"Соединено: {count}/4");
                if (count == 4) LogAssert.Expect(LogType.Log, "Мини-игра пройдена!");
                contact.OnEndDrag(pointer);
                Assert.That(Vector3.Distance(target.position, plug.position), Is.LessThan(0.1f));
                Assert.That(image.raycastTarget, Is.False);
            }
            Assert.That(game.IsGameEnded, Is.True);
            Assert.That(Field<int>(game, "connectedCount"), Is.EqualTo(4));
            Assert.That(game.BeginGame(), Is.True);
            foreach (var contact in contacts) Assert.That(contact.GetComponent<Image>().raycastTarget, Is.True);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
