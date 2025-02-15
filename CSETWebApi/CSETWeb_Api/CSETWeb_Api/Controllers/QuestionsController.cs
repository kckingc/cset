//////////////////////////////// 
// 
//   Copyright 2021 Battelle Energy Alliance, LLC  
// 
// 
//////////////////////////////// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CSET_Main.Views.Questions.QuestionDetails;
using CSETWeb_Api.BusinessManagers;
using CSETWeb_Api.Data.ControlData;
using CSETWeb_Api.Helpers;
using CSETWeb_Api.BusinessLogic.Helpers;
using CSETWeb_Api.Models;
using DataLayerCore.Model;
using Nelibur.ObjectMapper;
using CSET_Main.Data.AssessmentData;
using CSETWeb_Api.BusinessLogic.BusinessManagers;

namespace CSETWeb_Api.Controllers
{
    public class QuestionsController : ApiController
    {
        /// <summary>
        /// Returns a list of all applicable Questions or Requirements for the assessment.
        /// 
        /// A shorter list can be retrieved for a single Question_Group_Heading 
        /// by sending in the 'group' argument.  I'm not sure we need this yet. 
        /// </summary>
        [HttpPost]
        [Route("api/QuestionList")]
        public QuestionResponse GetList([FromBody] string group)
        {
            int assessmentId = Auth.AssessmentForUser();
            string applicationMode = GetApplicationMode(assessmentId);


            if (applicationMode.ToLower().StartsWith("questions"))
            {
                QuestionsManager qm = new QuestionsManager(assessmentId);
                QuestionResponse resp = qm.GetQuestionList(group);
                return resp;
            }
            else
            {
                RequirementsManager rm = new RequirementsManager(assessmentId);
                QuestionResponse resp = rm.GetRequirementsList();
                return resp;
            }
        }


        /// <summary>
        /// Returns a list of all Component questions, both default and overrides.
        /// </summary>
        [HttpPost]
        [Route("api/ComponentQuestionList")]
        public QuestionResponse GetComponentQuestionsList([FromBody] string group)
        {
            int assessmentId = Auth.AssessmentForUser();
            var manager = new ComponentQuestionManager(assessmentId);
            QuestionResponse resp = manager.GetResponse();
            return resp;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/QuestionListComponentOverridesOnly")]
        public QuestionResponse GetComponentOverridesList()
        {
            int assessmentId = Auth.AssessmentForUser();
            ComponentQuestionManager manager = new ComponentQuestionManager(assessmentId);
            QuestionResponse resp = manager.GetOverrideListOnly();
            return resp;

        }

        /// <summary>
        /// Sets the application mode to be question or requirements based.
        /// </summary>
        /// <param name="mode"></param>
        [HttpPost]
        [Route("api/SetMode")]
        public void SetMode([FromUri] string mode)
        {
            int assessmentId = Auth.AssessmentForUser();
            QuestionsManager qm = new QuestionsManager(assessmentId);
            qm.SetApplicationMode(mode);
        }


        /// <summary>
        /// Gets the application mode (question or requirements based).
        /// </summary>
        [HttpGet]
        [Route("api/GetMode")]
        public string GetMode()
        {
            int assessmentId = Auth.AssessmentForUser();
            QuestionsManager qm = new QuestionsManager(assessmentId);
            string mode = GetApplicationMode(assessmentId).Trim().Substring(0, 1);

            if (String.IsNullOrEmpty(mode))
            {
                mode = AssessmentModeData.DetermineDefaultApplicationModeAbbrev();
                SetMode(mode);
            }

            return mode;
        }


        /// <summary>
        /// Determines if the assessment is question or requirements based.
        /// </summary>
        /// <param name="assessmentId"></param>
        /// <returns></returns>
        public string GetApplicationMode(int assessmentId)
        {
            using (var db = new CSET_Context())
            {
                var mode = db.STANDARD_SELECTION.Where(x => x.Assessment_Id == assessmentId).Select(x => x.Application_Mode).FirstOrDefault();

                if (mode == null)
                {
                    mode = AssessmentModeData.DetermineDefaultApplicationModeAbbrev();
                    SetMode(mode);
                }

                return mode;
            }
        }



        /// <summary>
        /// Persists an answer.  This includes Y/N/NA/A as well as comments and alt text.
        /// </summary>
        [HttpPost]
        [Route("api/AnswerQuestion")]
        public int StoreAnswer([FromBody] Answer answer)
        {
            if (answer == null)
            {
                return 0;
            }

            if (String.IsNullOrWhiteSpace(answer.QuestionType))
            {
                if (answer.Is_Component)
                    answer.QuestionType = "Component";
                if(answer.Is_Maturity)                    
                    answer.QuestionType = "Maturity";
                if (answer.Is_Requirement)
                    answer.QuestionType = "Requirement";
                if (!answer.Is_Requirement && !answer.Is_Maturity && !answer.Is_Component)
                    answer.QuestionType = "Question";
            }

            int assessmentId = Auth.AssessmentForUser();
            string applicationMode = GetApplicationMode(assessmentId);

            if (answer.Is_Component)
            {
                QuestionsManager qm = new QuestionsManager(assessmentId);
                return qm.StoreComponentAnswer(answer);
            }

            if (answer.Is_Requirement)
            {
                RequirementsManager rm = new RequirementsManager(assessmentId);
                return rm.StoreAnswer(answer);
            }

            if (answer.Is_Maturity)
            {
                MaturityManager mm = new MaturityManager();
                return mm.StoreAnswer(assessmentId, answer);
            }

            QuestionsManager qm2 = new QuestionsManager(assessmentId);
            return qm2.StoreAnswer(answer);
        }


        /// <summary>
        /// Returns the details under a given questions details
        /// </summary>
        [HttpPost, HttpGet]
        [Route("api/Details")]
        public QuestionDetailsContentViewModel GetDetails([FromUri] int QuestionId, string questionType)
        {
            int assessmentId = Auth.AssessmentForUser();

            QuestionsManager qm = new QuestionsManager(assessmentId);
            return qm.GetDetails(QuestionId, questionType);
        }


        /// <summary>
        /// Persists a single answer to the SUB_CATEGORY_ANSWERS table for the 'block answer',
        /// and flips all of the constituent questions' answers.
        /// </summary>
        /// <param name="subCatAnswers"></param>
        [HttpPost]
        [Route("api/AnswerSubcategory")]
        public void StoreSubcategoryAnswers([FromBody] SubCategoryAnswers subCatAnswers)
        {
            int assessmentId = Auth.AssessmentForUser();

            QuestionsManager qm = new QuestionsManager(assessmentId);
            qm.StoreSubcategoryAnswers(subCatAnswers);
        }


        /// <summary>
        /// Note that this only populates the summary/title and finding id. 
        /// the rest is populated in a seperate call. 
        /// </summary>
        /// <param name="Answer_Id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/AnswerAllDiscoveries")]
        public List<Finding> AllDiscoveries([FromUri] int Answer_Id)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                FindingsViewModel fm = new FindingsViewModel(context, assessmentId, Answer_Id);
                return fm.AllFindings();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="Answer_Id"></param>
        /// <param name="Finding_id"></param>
        /// <param name="Question_Id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GetFinding")]
        public Finding GetFinding([FromUri] int Answer_Id, int Finding_id, int Question_Id, string QuestionType)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                if (Answer_Id == 0)
                {
                    QuestionsManager questions = new QuestionsManager(assessmentId);
                    Answer_Id = questions.StoreAnswer(new Answer()
                    {
                        QuestionId = Question_Id,
                        MarkForReview = false, 
                        QuestionType = QuestionType
                    });
                }
                FindingsViewModel fm = new FindingsViewModel(context, assessmentId, Answer_Id);
                return fm.GetFinding(Finding_id);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetImportance")]
        public List<Importance> GetImportance()
        {
            int assessmentId = Auth.AssessmentForUser();
            TinyMapper.Bind<IMPORTANCE, Importance>();
            List<Importance> rlist = new List<Importance>();
            using (CSET_Context context = new CSET_Context())
            {
                foreach (IMPORTANCE import in context.IMPORTANCE)
                {
                    rlist.Add(TinyMapper.Map<IMPORTANCE, Importance>(import));
                }
            }
            return rlist;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="Finding_Id"></param>
        [HttpPost]
        [Route("api/DeleteFinding")]
        public void DeleteFinding([FromBody] int Finding_Id)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                FindingViewModel fm = new FindingViewModel(Finding_Id, context);
                fm.Delete();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="finding"></param>
        [HttpPost]
        [Route("api/AnswerSaveDiscovery")]
        public void SaveDiscovery([FromBody] Finding finding)
        {
            int assessmentId = Auth.AssessmentForUser();
            using (CSET_Context context = new CSET_Context())
            {
                if (finding.IsFindingEmpty())
                {
                    DeleteFinding(finding.Finding_Id);
                    return;
                }

                FindingViewModel fm = new FindingViewModel(finding, context);
                fm.Save();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="question_id"></param>
        /// <param name="Component_Symbol_Id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/GetOverrideQuestions")]
        public List<Answer_Components_Exploded_ForJSON> GetOverrideQuestions([FromUri] int question_id, int Component_Symbol_Id)
        {
            int assessmentId = Auth.AssessmentForUser();

            ComponentQuestionManager manager = new ComponentQuestionManager(assessmentId);

            return manager.GetOverrideQuestions(assessmentId, question_id, Component_Symbol_Id);
        }


        /// <summary>
        /// this will explode the provided guid and 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="ShouldSave">true means explode and save false is delete these questions</param>
        [HttpGet]
        [Route("api/AnswerSaveComponentOverrides")]
        public void SaveComponentOverride([FromUri] String guid, Boolean ShouldSave)
        {
            int assessmentId = Auth.AssessmentForUser();
            string applicationMode = GetApplicationMode(assessmentId);

            ComponentQuestionManager manager = new ComponentQuestionManager(assessmentId);
            Guid g = new Guid(guid);
            manager.HandleGuid(g, ShouldSave);
        }


        /// <summary>
        /// Changes the title of a stored document.
        /// </summary>
        /// <param name="id">The document ID</param>
        /// <param name="title">The new title</param>
        [HttpPost]
        [Route("api/RenameDocument")]
        public void RenameDocument([FromUri] int id, string title)
        {
            int assessmentId = Auth.AssessmentForUser();
            new DocumentManager(assessmentId).RenameDocument(id, title);
        }


        /// <summary>
        /// Detaches a stored document from the answer.  
        /// </summary>
        /// <param name="id">The document ID</param>
        /// <param name="answerId">The document ID</param>
        [HttpPost]
        [Route("api/DeleteDocument")]
        public void DeleteDocument([FromUri] int id, int answerId)
        {
            int assessmentId = Auth.AssessmentForUser();
            new DocumentManager(assessmentId).DeleteDocument(id, answerId);
        }


        /// <summary>
        /// Returns a collection of all 
        /// </summary>
        /// <param name="id">The document ID</param>
        [HttpGet]
        [Route("api/QuestionsForDocument")]
        public List<int> GetQuestionsForDocument([FromUri] int id)
        {
            int assessmentId = Auth.AssessmentForUser();
            return new DocumentManager(assessmentId).GetQuestionsForDocument(id);
        }


        /// <summary>
        /// Returns a collection of Parameters with assessment-level overrides
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/ParametersForAssessment")]
        public List<ParameterToken> GetDefaultParametersForAssessment()
        {
            int assessmentId = Auth.AssessmentForUser();
            RequirementsManager rm = new RequirementsManager(assessmentId);

            return rm.GetDefaultParametersForAssessment();
        }


        /// <summary>
        /// Persists an assessment-level ("global") Parameter change.
        /// </summary>
        [HttpPost]
        [Route("api/SaveAssessmentParameter")]
        public ParameterToken SaveAssessmentParameter([FromBody] ParameterToken token)
        {
            int assessmentId = Auth.AssessmentForUser();
            RequirementsManager rm = new RequirementsManager(assessmentId);

            return rm.SaveAssessmentParameter(token.Id, token.Substitution);
        }


        /// <summary>
        /// Persists an answer-level ("in-line", "local") Parameter change.
        /// </summary>
        [HttpPost]
        [Route("api/SaveAnswerParameter")]
        public ParameterToken SaveAnswerParameter([FromBody] ParameterToken token)
        {
            int assessmentId = Auth.AssessmentForUser();
            RequirementsManager rm = new RequirementsManager(assessmentId);

            return rm.SaveAnswerParameter(token.RequirementId, token.Id, token.AnswerId, token.Substitution);
        }
    }
}


