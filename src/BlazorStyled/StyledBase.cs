﻿using BlazorStyled.Stylesheets;
using Microsoft.AspNetCore.Components;
using System;

namespace BlazorStyled
{
    public class StyledBase : ComponentBase, IObserver<IStyleSheet>, IDisposable
    {
        [Inject] protected IStyleSheet StyleSheet { get; private set; }
        private string _hashCode = null;
        private bool _shouldRender = true;

        private IDisposable _unsubscriber;

        protected override void OnInit()
        {
            _unsubscriber = StyleSheet.Subscribe(this);
        }

        protected override bool ShouldRender()
        {
            return _shouldRender;
        }

        public void OnCompleted()
        {
            _unsubscriber.Dispose();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(IStyleSheet value)
        {
            //Only call state has changed if the stylesheet really changed
            string newHashCode = StyleSheet.GetHashCodes();
            if (_hashCode != newHashCode)
            {
                _shouldRender = true;
                Invoke(StateHasChanged);
                _hashCode = newHashCode;
            }
            else
            {
                _shouldRender = false;
            }
        }

        public void Dispose()
        {
            _unsubscriber.Dispose();
        }
    }
}
