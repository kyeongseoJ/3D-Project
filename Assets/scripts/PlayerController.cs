using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 걷기 속도값
    [SerializeField]
    private float walkspeed;
    // 달리기 속도
    [SerializeField]
    private float runspeed;
    // 현재 적용중인 속도
    private float applyspeed;
    // 앉을 때 속도
    [SerializeField]
    private float crouchspeed;
    // 얼마만큼의 힘으로 위로 올라갈지 
    [SerializeField]
    private float jumpForce;

    // 상태 변수
    private bool isRun = false; // 걷기인지 달리기인지
    private bool isGround = false; // 땅인지 아닌지
    private bool isCrouch = false; // 앉았는지 아닌지

    // 앉았을 떄 얼마나 앉을지 결정하는 변수
    [SerializeField]
    private float crouchPosY; // 앉을 때 이동할 위치값
    private float originPosY; // 원래높이값
    private float applyCrouchPosY; //적용될 위치값


    // 바닥에 닿았는지 땅 착지여부를  확인할 콜라이더
    private CapsuleCollider capsuleCollider;

    // 카메라 민감도
    [SerializeField]
    private float lookSensitivity;

    // 카메라 움직이는 각도 영역 설정
    [SerializeField]
    private float cameraRotationLimit;
    // 현재 카메라 회전각 : 정면을 바라보기
    private float currentCameraRotationX = 0;

    // 카메라 컴포넌트 불러오기 :  플레이어 자식에 존재 
    [SerializeField]
    private Camera theCamera;

    // collider로 충돌영역 설정하고 rigidbody는 collider에 물리학을 입려준다.
    private Rigidbody myRigid;

    private void Start() {
        
        // 이렇게 안하고 SerializeField로 작성해서 직접 할당 theCamera = FindObjectOfType<Camera>();
        // 사용할 컴포넌트 초기화
        myRigid = GetComponent<Rigidbody>();
        // 시작시 기본속도는 걷기로 설정
        applyspeed = walkspeed;
        // 
        capsuleCollider =GetComponent<CapsuleCollider>();
        // 캐릭터가 움직이면 땅에 박힌다. 카메라의 위치가 이동해야한다.
        originPosY = theCamera.transform.localPosition.y;
        applyCrouchPosY = originPosY;
    }

    private void Update() {
        // 키 입력이 이뤄지면 실시간으로 움직여라
        Move();
        CameraRotataion(); // 상하 카메라 회전
        CharactorRotataion(); // 좌우 카메라 회전
        TryRun(); // 뛰고 있는지 확인하는 함수
        TryJump(); // 점프중인지 확인하는 함수  
        IsGround(); // isGround가 true인지 false인지 확인하는 함수 
        TryCrouch(); // 앉으려고 시도 
    }

    private void TryCrouch(){
        if(Input.GetKeyDown(KeyCode.LeftControl)){
            Crouch();
        }
    }
    // 앉기 혹은 서기
    private void Crouch(){
        // 상태전환
        isCrouch = !isCrouch;

        if(isCrouch){
            applyspeed = crouchspeed;
            applyCrouchPosY = crouchPosY;
        }
        else{
            applyspeed = walkspeed;
            applyCrouchPosY = originPosY;
        }
        // 카메라 이동 (카메라의 현재 x 값, 바뀔 y 값 , 카메라의 현재 z 값)
        //theCamera.transform.localPosition = new Vector3(theCamera.transform.localPosition.x, applyCrouchPosY, theCamera.transform.localPosition.z);
        // 위의 코드를 대기시간 줘서 자연스럽게 앉는 느낌을 주었다.
        StartCoroutine(CrouchCoroutine());
    }

    // 부드러운 카메라 시점 이동을 위한 코루틴
    IEnumerator CrouchCoroutine(){
        float _posY = theCamera.transform.localPosition.y;
        // 임시변수 생성 보간법 적용 시 계속해서 코루틴이 실행되게 된다
        int count = 0;

        while(_posY != applyCrouchPosY){
            count++;
            // (1,2,0.5) 1에서 1까지 0.5의 비율로 증가, 보간법을 이용해 자연스럽게 시점 높이 변경
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.2f); 
            // 변경된 posY를 적용 => 벡터를 이용해 값을 변경 Y만 바뀌면 된다.
            theCamera.transform.localPosition = new Vector3(0, _posY, 0);
            // 일정횟수 실행되면 코루틴 종료되게 하는 조건문 작성
            if(count > 20) 
                break;
            yield return null;// 대기시간적용 : 한프레임 대기
        }   
        // count만큼 반복하다가 목적지에 도달하게 되면 끝나도록 설정
        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0f);
    }
    // 지면체크
    private void IsGround(){
        // 캡슐의 위치에서 레이저를 쏜다 + 아래방향으로 고정된 좌표로 구현,
        // y의 1/2크기만큼의 콜라이더 영역으로 땅 사이의 거리 만큼 레이저를 쏜다, + 0.1f로 대각선 이동 시 여유공간 확보
        isGround = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f);
    }
    // 점프 시도
    private void TryJump(){
        if (Input.GetKeyDown(KeyCode.Space) && isGround){
            Jump();
        }
    }
    // 점프
    private void Jump(){
        // 앉은 상태에서 점프 시 앉기를 해제
        if(isCrouch) // true일 경우 crouch cancel
            Crouch();
        // 공중 방향으로 jumpForce만큼 이동
        myRigid.velocity = transform.up * jumpForce;

    }
    // 달리기 시도
    private void TryRun(){
        if (Input.GetKey(KeyCode.LeftShift)){
            Running();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift)){
            RunningCancel();
        }
    }
    // 달리기 함수 
    private void Running(){
        isRun =true;
        applyspeed = runspeed;
    }
    // 걷기로 변환
    private void RunningCancel(){
        isRun = false;
        applyspeed = walkspeed;
    }
    private void Move(){
        // 좌우/ 앞뒤 이동
        float _moveDirX = Input.GetAxisRaw("Horizontal");
        float _moveDirZ = Input.GetAxisRaw("Vertical");

        Vector3 _moveHorizontal = transform.right * _moveDirX; // (-1,0,0)
        Vector3 _moveVertical = transform.forward * _moveDirZ; // (0,0,-1)
        // 한번 누를때 마다 움직일 거리 = 이동할 방향 * 속도
        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applyspeed; 
        // 합이 1이 나오도록 정규화 : 1초에 얼마나 움직일지 계산이 편해진다.

        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime);
    }

   // 카메라 상하 이동
    private void CameraRotataion(){
        // 마우스 이동은 2차원 x,y만 존재  위아래로 고개를 움직이는 느낌
        float _xRotation = Input.GetAxisRaw("Mouse Y");
        // 카메라 시선 이동 민감도 조절 :  천천히 움직이도록
        float _cameraRotationX = _xRotation * lookSensitivity;
        currentCameraRotationX -= _cameraRotationX; // += 이면 마우스 방향과 반대로 움직이는 카메라이동을 구현가능하다.
        // -45도 +45도 사이로 고정
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);
        // 마우스 위아래로만 움직이게 설정
        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
    }
    // 카메라 좌우 
    private void CharactorRotataion(){
        // X축 기준으로 회전
        float _yRotation = Input.GetAxisRaw("Mouse X");
        // 카메라 민감도 조절
        Vector3 _chractorRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        myRigid.MoveRotation(myRigid.rotation * Quaternion.Euler(_chractorRotationY));

        // 오일러 회전각 비교해서 확인
        //Debug.Log(myRigid.rotation);
        //Debug.Log(myRigid.rotation.eulerAngles);
    }


}