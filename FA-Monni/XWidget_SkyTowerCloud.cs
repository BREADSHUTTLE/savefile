using UnityEngine;
using System.Collections;

public class XWidget_SkyTowerCloud : MonoBehaviour
{
    public UIScrollView m_listScrollView = null;
    public XPanel_SkyTower pnlSkyTower = null;
    public Camera m_Camera = null;
    public float m_Speed;

    private float currPosY = 0;

    private bool updateState = false;

    public void BuildCloud(float speed)
    {
        if (pnlSkyTower != null)
        {
            if (m_Camera == null)
                m_Camera = pnlSkyTower.GetParentCamera();
        }

        currPosY = m_listScrollView.panel.clipOffset.y;
        updateState = true;

        m_Speed = (speed / 100);
    }

    void Update()
    {
        if (updateState)
        {
            var scrollBar = m_listScrollView.panel.clipOffset.y;
            if (currPosY != scrollBar)
            {
                gameObject.transform.localPosition += new Vector3(0f, (currPosY - scrollBar) * m_Speed, 0f);
                currPosY = m_listScrollView.panel.clipOffset.y;
            }

            Vector3 pos = m_Camera.WorldToViewportPoint(gameObject.transform.position);

            if (pos.y < -2f)
                pos.y = 2f;

            if (pos.y > 2f)
                pos.y = -2f;

            gameObject.transform.position = m_Camera.ViewportToWorldPoint(pos);
        }
    }
}