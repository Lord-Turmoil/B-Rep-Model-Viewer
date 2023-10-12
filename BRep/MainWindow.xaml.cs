// Copyright (C) 2018 - 2023 Tony's Studio. All rights reserved.

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using BRep.Extensions;
using BRep.Model;
using Microsoft.Win32;

namespace BRep;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const double RotationThreshold = 0.1;

    // Whether current model is default or not.
    // Set it to false so that we can load default model on startup.
    private bool _isDefault;

    // Whether in simple mode.
    private bool _isSimple;

    private Point _lastRotatePos;
    private Point _lastTranslatePos;

    private bool _leftDown;
    private BRepModel? _model;
    private BRepModelGroup _modelGroup = new();

    private ModelLoader _modelLoader = new SimpleModelLoader();

    private bool _rightDown;
    private double _totalRotation;

    public MainWindow()
    {
        InitializeComponent();
        LoadDefaultModel();
    }

    private void LoadDefaultModel()
    {
        if (_isDefault)
        {
            return;
        }

        ClearModel();
        _modelGroup = ModelLoader.LoadDefault();
        _modelGroup.Accept(model => Group.Children.Add(model));
        _isDefault = true;
        ModelName.Content = "Default Model";
        _model = null;
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        Camera.Position = new Point3D(Camera.Position.X, Camera.Position.Y, 5);
        // _modelGroup.Accept(model => model.Transform = new Transform3DGroup());
        _modelGroup.ResetTransform();
    }

    private void LoadButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json"
        };
        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        LoadModel(openFileDialog.FileName);
    }

    private void LoadModel(string filename)
    {
        try
        {
            _modelGroup = _modelLoader.LoadFromJson(filename);
            ClearModel();
            _modelGroup.Accept(model => Group.Children.Add(model));
            _model = _modelLoader.Model;
            _isDefault = false;
            ReorderModels();
            ModelName.Content = Path.GetFileNameWithoutExtension(filename);
        }
        catch (Exception ex)
        {
            string message = $"Failed to load model from file: {filename}";
            message += '\n' + ex.Message;
            MessageBox.Show(message, "Failed to load model");
        }
    }

    private void LoadDefaultButton_OnClick(object sender, RoutedEventArgs e)
    {
        LoadDefaultModel();
    }

    private void ClearModel()
    {
        Group.Children.Clear();
        Group.Children.Add(new AmbientLight(Colors.DarkGray));
        Group.Children.Add(new DirectionalLight(Colors.Gray, new Vector3D(-1, -1, -1)));
    }

    private void ChangeColorButton_OnClick(object sender, RoutedEventArgs e)
    {
        Reload();
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        _isSimple = false;
        _modelLoader = new ColorfulModelLoader();
        Reload();
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        _isSimple = true;
        _modelLoader = new SimpleModelLoader();
        Reload();
    }

    private void Reload()
    {
        if (_isDefault || _model == null)
        {
            return;
        }

        try
        {
            _modelGroup = _modelLoader.LoadFromModel(_model);
            ClearModel();
            _modelGroup.Accept(model => Group.Children.Add(model));
            ReorderModels();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Failed to reload model");
        }
    }

    private void Display_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Click left button to rotate, right button to translate.
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _leftDown = true;
            Point pos = Mouse.GetPosition(Viewport);
            _lastRotatePos = new Point(pos.X - Viewport.ActualWidth / 2, Viewport.ActualHeight / 2 - pos.Y);
            Viewport.Cursor = Cursors.SizeAll;
            Display.Cursor = Cursors.SizeAll;
        }

        if (e.RightButton == MouseButtonState.Pressed)
        {
            _rightDown = true;
            Point pos = Mouse.GetPosition(Viewport);
            _lastTranslatePos = new Point(pos.X - Viewport.ActualWidth / 2, Viewport.ActualHeight / 2 - pos.Y);
            Viewport.Cursor = Cursors.SizeAll;
            Display.Cursor = Cursors.SizeAll;
        }
    }

    private void Display_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        Viewport.Cursor = Cursors.Arrow;
        Display.Cursor = Cursors.Arrow;
        _leftDown = false;
        _rightDown = false;
    }

    private void Display_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Camera.Position = new Point3D(Camera.Position.X, Camera.Position.Y, Camera.Position.Z - e.Delta / 250D);
    }

    private void Display_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_leftDown)
        {
            OnRotate();
        }

        if (_rightDown)
        {
            OnTranslate();
        }
    }

    private void OnRotate()
    {
        Point pos = Mouse.GetPosition(Viewport);
        var actualPos = new Point(pos.X - Viewport.ActualWidth / 2, Viewport.ActualHeight / 2 - pos.Y);

        // Get the angle between the two vectors.
        double dx = actualPos.X - _lastRotatePos.X;
        double dy = actualPos.Y - _lastRotatePos.Y;
        double mouseAngle = 0;
        if (dx != 0 && dy != 0)
        {
            mouseAngle = Math.Asin(Math.Abs(dy) / Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2)));
            switch (dx)
            {
                case < 0 when dy > 0:
                    mouseAngle += Math.PI / 2;
                    break;
                case < 0 when dy < 0:
                    mouseAngle += Math.PI;
                    break;
                case > 0 when dy < 0:
                    mouseAngle += Math.PI * 1.5;
                    break;
            }
        }
        else if (dx == 0 && dy != 0)
        {
            mouseAngle = Math.Sign(dy) > 0 ? Math.PI / 2 : Math.PI * 1.5;
        }
        else if (dx != 0 && dy == 0)
        {
            mouseAngle = Math.Sign(dx) > 0 ? 0 : Math.PI;
        }

        // Rotate axis.
        double axisAngle = mouseAngle + Math.PI / 2;
        var axis = new Vector3D(Math.Cos(axisAngle) * 4, Math.Sin(axisAngle) * 4, 0);
        // To prevent too much rotation, we limit the rotation angle to 0.01 * distance.
        double rotation = 0.01 * Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));

        _modelGroup.Accept(model =>
        {
            var group = model.Transform as Transform3DGroup;
            var rotationParam = new QuaternionRotation3D(new Quaternion(axis, rotation * 180 / Math.PI));
            group?.Children.Add(new RotateTransform3D(rotationParam));
        });

        _lastRotatePos = actualPos;

        _totalRotation += rotation;
        if (Math.Abs(_totalRotation) > RotationThreshold)
        {
            _totalRotation = 0.0;
            ReorderModels();
        }
    }

    private void OnTranslate()
    {
        Point pos = Mouse.GetPosition(Viewport);
        var actualPos = new Point(pos.X - Viewport.ActualWidth / 2, Viewport.ActualHeight / 2 - pos.Y);

        double dx = (actualPos.X - _lastTranslatePos.X) * 0.01;
        double dy = (actualPos.Y - _lastTranslatePos.Y) * 0.01;

        _modelGroup.Accept(model =>
        {
            var group = model.Transform as Transform3DGroup;
            group?.Children.Add(new TranslateTransform3D(dx, dy, 0));
        });

        _lastTranslatePos = actualPos;
    }

    private void ReorderModels()
    {
        if (!_isDefault && !_isSimple)
        {
            Group.AlphaSortModels(Camera);
        }
    }

    private void ExitMenu_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void AboutMenu_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(StaticResource.AboutMessage, "About B-Rep Model Viewer");
    }

    private void HelpMenu_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(StaticResource.HelpMessage, "B-Rep Model Viewer Help");
    }
}