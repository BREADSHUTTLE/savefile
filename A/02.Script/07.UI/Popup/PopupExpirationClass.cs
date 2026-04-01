using System;
using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class PopupExpirationClass : BasePopup
    {
        [Header("Info")]
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private TMP_Text txtDesc;
        [SerializeField] private TMP_Text txtClassName;

        [Header("Button")]
        [SerializeField] private CPButton btnOneOk;
        [SerializeField] private CPButton btnCancel;
        [SerializeField] private CPButton btnOk;

        public void SetData(string _title, string _desc, string _name, Action _action)
        {
            if (txtTitle != null)
                txtTitle.text = _title;
            if (txtDesc != null)
                txtDesc.text = _desc;
            if (txtClassName != null)
                txtClassName.text = _name;

            if (btnOneOk != null)
                btnOneOk.gameObject.SetActive(false);

            if (btnCancel != null)
            {
                btnCancel.gameObject.SetActive(true);
                btnCancel.onClick.RemoveAllListeners();
                btnCancel.onClick.AddListener(Close);
            }

            if (btnOk != null)
            {
                btnOk.gameObject.SetActive(true);
                btnOk.onClick.RemoveAllListeners();
                btnOk.onClick.AddListener(() => { _action(); });
            }
        }

        public void SetDataConfirmOnly(string _title, string _desc, string _name)
        {
            if (txtTitle != null)
                txtTitle.text = _title;
            if (txtDesc != null)
                txtDesc.text = _desc;
            if (txtClassName != null)
                txtClassName.text = _name;

            if (btnCancel != null)
                btnCancel.gameObject.SetActive(false);

            if (btnOk != null)
                btnOk.gameObject.SetActive(false);

            if (btnOneOk != null)
            {
                btnOneOk.gameObject.SetActive(true);
                btnOneOk.onClick.RemoveAllListeners();
                btnOneOk.onClick.AddListener(Close);
            }
        }
    }
}