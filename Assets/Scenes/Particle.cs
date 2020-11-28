using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public partial class Particle : MonoBehaviour
{
    [SerializeField]
    int _maxParticles = 100000;

    [SerializeField]
    Vector3 _emitterCenter = Vector3.zero;

    [SerializeField]
    Vector3 _emitterSize = Vector3.one;

    [SerializeField]
    float _life = 4.0f;

    [SerializeField, Range(0, 1)]
    float _lifeRandomness = 0.6f;

    [SerializeField]
    Vector3 _initialVelocity = Vector3.forward * 4.0f;

    [SerializeField]
    Vector3 _acceleration = Vector3.zero;

    [SerializeField]
    Material _material;

    [SerializeField]
    Shader _kernelShader;

    Material _kernelMaterial;

    [SerializeField]
    Mesh[] _shapes = new Mesh[1];

    RenderTexture _positionBuffer1;
    RenderTexture _positionBuffer2;
    RenderTexture _velocityBuffer1;
    RenderTexture _velocityBuffer2;
    RenderTexture _rotationBuffer1;
    RenderTexture _rotationBuffer2;

    CombineMesh _mesh;
    MaterialPropertyBlock _props;

    static float deltaTime
    {
        get
        {
            var isEditor = !Application.isPlaying || Time.frameCount < 2;
            return isEditor ? 1.0f / 10 : Time.deltaTime;
        }
    }

    Material CreateMaterial(Shader shader)
    {
        var material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        return material;
    }

    RenderTexture CreateBuffer()
    {
        var width = _mesh.copyCount;
        var height = _maxParticles / width + 1;
        var buffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        buffer.hideFlags = HideFlags.DontSave;
        buffer.filterMode = FilterMode.Point;
        buffer.wrapMode = TextureWrapMode.Repeat;
        return buffer;
    }

    void UpdateKernelShader()
    {
        var m = _kernelMaterial;

        m.SetVector("_EmitterPos", _emitterCenter);
        m.SetVector("_EmitterSize", _emitterSize);

        var invLifeMax = 1.0f / Mathf.Max(_life, 0.01f);
        var invLifeMin = invLifeMax / Mathf.Max(1 - _lifeRandomness, 0.01f);
        m.SetVector("_LifeParams", new Vector2(invLifeMin, invLifeMax));

        if (_initialVelocity == Vector3.zero)
        {
            m.SetVector("_Direction", new Vector4(0, 0, 1, 0));
            m.SetVector("_SpeedParams", Vector4.zero);
        }
        else
        {
            var speed = _initialVelocity.magnitude;
            var dir = _initialVelocity / speed;
            m.SetVector("_Direction", new Vector4(dir.x, dir.y, dir.z, 0));
            m.SetVector("_SpeedParams", new Vector2(speed, 0));
        }

        var aparams = new Vector4(_acceleration.x, _acceleration.y, _acceleration.z, 0);
        m.SetVector("_Acceleration", aparams);

        m.SetVector("_Config", new Vector4(deltaTime, Time.time, 0, 0));
    }

    void InitializeAndPrewarmBuffers()
    {
        UpdateKernelShader();

        Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 0);
        Graphics.Blit(null, _velocityBuffer2, _kernelMaterial, 1);
        Graphics.Blit(null, _rotationBuffer2, _kernelMaterial, 2);

        for (var i = 0; i < 8; i++)
        {
            SwapBuffersAndInvokeKernels();
            UpdateKernelShader();
        }
    }

    void SwapBuffersAndInvokeKernels()
    {
        // Swap the buffers.
        var tempPosition = _positionBuffer1;
        var tempVelocity = _velocityBuffer1;
        var tempRotation = _rotationBuffer1;

        _positionBuffer1 = _positionBuffer2;
        _velocityBuffer1 = _velocityBuffer2;
        _rotationBuffer1 = _rotationBuffer2;

        _positionBuffer2 = tempPosition;
        _velocityBuffer2 = tempVelocity;
        _rotationBuffer2 = tempRotation;

        // Invoke the position update kernel.
        _kernelMaterial.SetTexture("_PositionBuffer", _positionBuffer1);
        _kernelMaterial.SetTexture("_VelocityBuffer", _velocityBuffer1);
        _kernelMaterial.SetTexture("_RotationBuffer", _rotationBuffer1);
        Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 3);

        // Invoke the velocity and rotation update kernel
        // with the updated position.
        _kernelMaterial.SetTexture("_PositionBuffer", _positionBuffer2);
        Graphics.Blit(null, _velocityBuffer2, _kernelMaterial, 4);
        Graphics.Blit(null, _rotationBuffer2, _kernelMaterial, 5);
    }

    void Init()
    {
        if (_positionBuffer1)
        {
            return;
        }

        if (_mesh == null)
        {
            _mesh = new CombineMesh(_shapes);
        }
        else
        {
            _mesh.Rebuild(_shapes);
        }

        _positionBuffer1 = CreateBuffer();
        _positionBuffer2 = CreateBuffer();
        _velocityBuffer1 = CreateBuffer();
        _velocityBuffer2 = CreateBuffer();
        _rotationBuffer1 = CreateBuffer();
        _rotationBuffer2 = CreateBuffer();

        _kernelMaterial = CreateMaterial(_kernelShader);

        InitializeAndPrewarmBuffers();
    }

    void Update()
    {
        Init();

        UpdateKernelShader();
        SwapBuffersAndInvokeKernels();

        if (_props == null)
        {
            _props = new MaterialPropertyBlock();
        }
        var props = _props;
        props.SetTexture("_PositionBuffer", _positionBuffer2);
        props.SetTexture("_RotationBuffer", _rotationBuffer2);

        var mesh = _mesh.mesh;
        var position = transform.position;
        var rotation = transform.rotation;
        var uv = new Vector2(0, 0);

        for (var i = 0; i < _positionBuffer2.height; i++)
        {
            uv.y = (0.5f + i) / _positionBuffer2.height;
            props.SetVector("_BufferOffset", uv);
            Graphics.DrawMesh(
                mesh, position, rotation,
                _material, 0, null, 0, props,
                false, false);
        }

    }


}
