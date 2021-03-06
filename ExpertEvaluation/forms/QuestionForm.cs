﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExpertEvaluation.classes;

namespace ExpertEvaluation.forms
{
    public partial class QuestionForm : Form
    {
        private static Dictionary<QuestionType, System.Windows.Forms.Panel> _questionPanelDictionary;
        private readonly AdminForm _parentForm;
        private readonly Question _question;
        private QuestionType _selectedQuestionType;

        public QuestionForm(AdminForm parentForm)
        {
            InitializeComponent();
            InitializeQuestionPanelDictionary();
            this._question = new Question {QuestionNumber = Dao.MaxQuestionNumber()};
            this._parentForm = parentForm;
        }

        public QuestionForm(AdminForm parentForm, Question question)
        {
            InitializeComponent();
            InitializeQuestionPanelDictionary();
            this._question = question;
            this._parentForm = parentForm;

            BindEntityToAttributes();
        }

        private void InitializeQuestionPanelDictionary()
        {
            _questionPanelDictionary = new Dictionary<QuestionType, Panel>()
            {
                {QuestionType.BooleanQuestion, booleanQuestionPanel},
                {QuestionType.OneOfMany,oneOfManyQuestionPanel},
                {QuestionType.ManyOfMany,manyOfManyQuestionPanel},
                {QuestionType.NumberQuestion, numberQuestionPanel},
                {QuestionType.Interval,intervalQuestionPanel}
            };
        }

        private void QuestionForm_Load(object sender, EventArgs e)
        {
            foreach (var questionType in Question.GetQuestionTypes())
            {
                var questionTypeName = Question.QuestionDictionary[questionType];
                questionTypeCB.Items.Add(questionTypeName);
            }
            questionTypeCB.SelectedIndex = questionTypeCB.Items.IndexOf(Question.QuestionDictionary[_selectedQuestionType]);
        }

        private void questionTypeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedText = questionTypeCB.SelectedItem.ToString().TrimEnd();
            _selectedQuestionType = 
                Question.QuestionDictionary.First(x => x.Value.Equals(selectedText)).Key;
            foreach (var panel in _questionPanelDictionary.Values)
            {
                panel.Visible = false;
            }
            var selectedPanel = _questionPanelDictionary[_selectedQuestionType];
            selectedPanel.Visible = true;
        }

        private void ReturnToParent()
        {
            this.Hide();
            _parentForm.Show();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            ReturnToParent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (ValidateFields())
            {
                BindAttributesToEntity();
                Dao.SaveQuestion(_question);
                ReturnToParent();
            }
        }

        private bool ValidateFields()
        {
            int a, b; // used for int.TryParse method only
            if (QuestionTextBox.Text.Length<1)
            {
                MessageBox.Show(@" Please enter question text", @"Cannot create question",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!(int.TryParse(weightTB.Text,out a) && a>=0 && a<=10))
            {
                MessageBox.Show(@"Wrong question weight (must be from 0 to 10)", @"Cannot create question",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_selectedQuestionType==QuestionType.BooleanQuestion && !trueRB.Checked && !falseRB.Checked)
            {
                MessageBox.Show(@"No right answer chosen", @"Cannot create question",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_selectedQuestionType==QuestionType.OneOfMany && !oneOfManyRTB.Lines.Contains(oneOfManyCB.Text))
            {
                MessageBox.Show(@"Right answer chosen is not one of possible answers", @"Cannot create question",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_selectedQuestionType == QuestionType.ManyOfMany && !ManyOfManyIsValid())
            {
                MessageBox.Show(@"No right answer chosen", @"Cannot create question",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_selectedQuestionType == QuestionType.NumberQuestion && !int.TryParse(numberQuestionTB.Text, out a))
            {
                MessageBox.Show(@"Incorrect right answer", @"Cannot create question",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_selectedQuestionType == QuestionType.Interval && !(int.TryParse(intervalFromQuestionTB.Text, out a) && int.TryParse(intervalToQuestionTB.Text,out b)))
            {
                MessageBox.Show(@"Incorrect right answer", @"Cannot create question",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private bool ManyOfManyIsValid()
        {
            var rightAnswers = manyOfManyCLB.CheckedItems.Count;
            return rightAnswers >= 1;
        }

        private void BindEntityToAttributes()
        {
            QuestionTextBox.Text = _question.QuestionText;
            _selectedQuestionType = _question.QuestionType;
            weightTB.Text = _question.Weight.ToString();
            switch (_selectedQuestionType)
            {
                case QuestionType.BooleanQuestion:
                    if (_question.RightAnswers.Contains("True"))
                    {
                        trueRB.Checked = true;
                        falseRB.Checked = false;
                    }
                    else
                    {
                        trueRB.Checked = false;
                        falseRB.Checked = true;
                    }
                    break;
                case QuestionType.OneOfMany:
                    oneOfManyRTB.Lines = _question.PossibleAnswers.ToArray();
                    UpdateOneOfManyCB();
                    oneOfManyCB.Text = _question.RightAnswers[0];
                    break;
                case QuestionType.ManyOfMany:
                    manyOfManyCLB.Items.Clear();
                    manyOfManyCLB.Items.AddRange(items: _question.PossibleAnswers.ToArray());
                    foreach (var rightAnswer in _question.RightAnswers)
                    {
                        var index = manyOfManyCLB.Items.IndexOf(rightAnswer);
                        manyOfManyCLB.SetItemChecked(index, true);
                    }
                    break;
                case QuestionType.NumberQuestion:
                    numberQuestionTB.Text = _question.GetRightAnswers();
                    break;
                case QuestionType.Interval:
                    String[] answers = _question.GetRightAnswers().Split(';');
                    intervalFromQuestionTB.Text = answers[0];
                    intervalToQuestionTB.Text = answers[1];
                    break;
            }
        }

        private void BindAttributesToEntity()
        {
            _question.QuestionText = QuestionTextBox.Text;
            _question.QuestionType = _selectedQuestionType;
            _question.Weight = int.Parse(weightTB.Text);
            switch (_selectedQuestionType)
            {
                case QuestionType.BooleanQuestion:
                    _question.PossibleAnswers = new List<string>() { "True", "False" };
                    _question.RightAnswers = new List<string>() { trueRB.Checked ? "True" : "False" };
                    break;
                case QuestionType.OneOfMany:
                    _question.PossibleAnswers = new List<string>();
                    foreach (var line in oneOfManyRTB.Lines.Where(line => line.Length > 0))
                    {
                        _question.PossibleAnswers.Add(line);
                    }
                    _question.RightAnswers = new List<string>(){oneOfManyCB.Text};
                    break;
                case QuestionType.ManyOfMany:
                    _question.PossibleAnswers = new List<string>();
                    foreach (var item in manyOfManyCLB.Items)
                    {
                        _question.PossibleAnswers.Add(item.ToString());
                    }
                    _question.RightAnswers = new List<string>();
                    foreach (var checkedItem in manyOfManyCLB.CheckedItems)
                    {
                        _question.RightAnswers.Add(checkedItem.ToString());
                    }
                    break;
                case QuestionType.NumberQuestion:
                    _question.RightAnswers = new List<string>() {numberQuestionTB.Text};
                    break;
                case QuestionType.Interval:
                    _question.RightAnswers = new List<string>()
                    {
                        intervalFromQuestionTB.Text,
                        intervalToQuestionTB.Text
                    };
                    break;
            }
        }

        private void oneOfManyRTB_TextChanged(object sender, EventArgs e)
        {
            UpdateOneOfManyCB();
        }

        private void UpdateOneOfManyCB()
        {
            oneOfManyCB.Items.Clear();
            foreach (var line in oneOfManyRTB.Lines.Where(line => line.Length > 0))
            {
                oneOfManyCB.Items.Add(line);
            }
        }

        private void manyofManyAddButton_Click(object sender, EventArgs e)
        {
            var answer = manyOfManyTB.Text;
            manyOfManyCLB.Items.Add(answer);
        }

        private void manyOfManyDeleteButton_Click(object sender, EventArgs e)
        {
            var checkedItems = new List<int>();
            foreach (var item in manyOfManyCLB.CheckedItems)
            {
                checkedItems.Add(manyOfManyCLB.Items.IndexOf(item));
            }
            foreach (var index in checkedItems.OrderByDescending(i=>i))
            {
                manyOfManyCLB.Items.RemoveAt(index);
            }
        }
    }
}
