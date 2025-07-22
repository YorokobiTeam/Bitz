using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class LoginScreenController : MonoBehaviour
{
    VisualElement root;
    TextField emailInput,
        passwordInput;

    Button loginBtn,
        registerBtn;

    public UserData userData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        this.root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("Root");
        this.emailInput = root.Q<TextField>("EmailInput");
        this.passwordInput = root.Q<TextField>("PasswordInput");
        this.loginBtn = root.Q<Button>("LoginButton");
        this.registerBtn = root.Q<Button>("RegisterButton");

        this.loginBtn.clicked += LoginBtn_onClick;
        this.registerBtn.clicked += RegisterBtn_clicked;
    }

    private void RegisterBtn_clicked()
    {

    }


    private async void LoginBtn_onClick()
    {
        var session = await BitzAccountService.GetInstance().Login(emailInput.text, passwordInput.text);
        if (session != null)
        {
            this.userData.playerInfo = await SupabaseProvider.GetInstance().From<BitzPlayerInfo>().Select("*").Single();
            this.userData.email = this.emailInput.text;
            LeanTween.alpha(this.gameObject, 0, 2f).setOnComplete(() =>
            {
                this.gameObject.SetActive(false);
            });
        }
    }


}
