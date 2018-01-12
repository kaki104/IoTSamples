using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnglishPractice2.Models;
using Microsoft.Toolkit.Uwp.Helpers;

namespace EnglishPractice2.Helpers
{
    public class SentenceHelper
    {
        private const string FILE_NAME = "sentence1.csv";

        /// <summary>
        /// 케이스 목록
        /// </summary>
        public IList<Sentence> SentenceList { get; set; } = new List<Sentence>();

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public SentenceHelper()
        {
            InitializeSentence();
        }

        /// <summary>
        /// 케이스 초기화
        /// </summary>
        private async void InitializeSentence()
        {
            var storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            if (await storageFolder.FileExistsAsync(FILE_NAME) == false)
            {
                throw new FileNotFoundException(FILE_NAME + "찾을 수 없습니다.");
            }

            var fileText = await storageFolder.ReadTextFromFileAsync(FILE_NAME);
            var lines = fileText.Replace("\r\n","\n").Split('\n');

            SentenceList?.Clear();

            var results = (from item in lines
                where item.Length > 0
                let columns = item.Split(',')
                let sentence = new Sentence
                {
                    Index = Convert.ToInt16(columns[0]),
                    ShowText = columns[1],
                    SpeakText = columns[2]
                }
                select AddItem(SentenceList as IList, sentence)).Count();

        }

        /// <summary>
        /// 아이템 추가
        /// </summary>
        /// <param name="list"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool AddItem(IList list, object item)
        {
            if (list == null
                || item == null) return false;
            list.Add(item);
            return true;
        }
    }
}
