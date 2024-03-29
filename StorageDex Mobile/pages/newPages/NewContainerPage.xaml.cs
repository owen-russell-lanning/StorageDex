﻿using FFImageLoading.Forms;
using Plugin.Media.Abstractions;
using StorageDex_Mobile.elements;
using StorageDex_Mobile.lib;
using StorageDex_Mobile.lib.interfaces;
using StorageDex_Mobile.lib.interfaces.toolbar;
using StorageDex_Mobile.pages.miscPages;
using StorageDexLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace StorageDex_Mobile.pages.newPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewContainerPage : ContentPage, INewPage, IRefreshable, IConfirmable, IToolbarDelete
    {
        /**
         * a page to create a new container
         */

        public MediaFile finalImage;
        private int newContainerId; //the id of the new container - generated by the database
        private StorageContainer previousContainer;
        private bool isEditing = false; //if the page is being used to edit a container
        private TagDisplay tagDisplay;
        private StorageLocation location;


        //takes the location that the new container will be in
        public NewContainerPage(StorageLocation location)
        {
            this.location = location;
            InitializeComponent();

            newContainerId = DatabaseHandler.GetDatabase().GenerateNewContainerId(); //get the id for the new container

            //set colors
            this.BackgroundColor = PageColors.secondaryColor;

            InitContent();
            tagDisplay = new TagDisplay();
            this.tagDisplayWrapper.Children.Add(tagDisplay);


        }

        //for editing a preexisting container
        public NewContainerPage(string name, string notes, List<string> tags, int id, StorageContainer prevContainer, StorageLocation location) : base()
        {
            this.location = location;
            InitializeComponent();
            InitContent();
            newContainerId = id;
            this.nameInput.Text = name;
            this.notesEditor.Text = notes;

            this.previousContainer = prevContainer;
            this.isEditing = true;

            if (isEditing)
            {
                this.Title = "Editing";
            }

            tagDisplay = new TagDisplay();
            this.tagDisplayWrapper.Children.Add(tagDisplay);
            tagDisplay.AddTagBatch(tags);

            //set colors
            this.BackgroundColor = PageColors.secondaryColor;

            InitContent();

            //set image
            if (ImageTools.HasImage(previousContainer))
            {

                if (ImageBaseHandler.current.isContainerCached(previousContainer.GetId()))
                {
                    SetImageHolderContent(ImageBaseHandler.current.GetContainerImageSource(previousContainer.GetId()));
                }
                else
                {
                    ImageSource imageLoadTask = ImageTools.LoadImage(System.IO.Path.Combine(Directories.locationImageDirectory, id.ToString() + ".jpg"));
                    SetImageHolderContent(imageLoadTask);
                }



            }
        }


        //initializes the content on the page 
        private void InitContent()
        {

            this.containerName.TextColor = PageColors.textColor;
            this.nameInput.TextColor = PageColors.textColor;
            this.tags.TextColor = PageColors.textColor;
            this.tagsInput.TextColor = PageColors.textColor;
            this.notes.TextColor = PageColors.textColor;
            this.notesEditor.TextColor = PageColors.textColor;

          


        }

        //handles the tag input box
        //moves the new tag onto the line below when it is finished
        private void TagInputOnType(Object sender, TextChangedEventArgs args)
        {
            string text = tagsInput.Text;
            if (text.Trim() == ",")
            {
                tagsInput.Text = "";
            }
            else if (text.EndsWith(",")) //if ends with comma, tag is finished
            {
                string newTag = text.Substring(0, text.Length - 1);
                TagButton newTagButton = new TagButton(newTag);
                tagDisplay.AddTag(newTagButton);
                tagsInput.Text = "";

            }
        }

        //handles the enter key being pressed on tag input 
        private void TagInputCompleted(Object sender, EventArgs args)
        {
            string text = tagsInput.Text;
            if (text.Trim() == "," || text.Trim() == "")
            {
                tagsInput.Text = "";
            }
            else
            {
                tagsInput.Text = "";
                TagButton newTagButton = new TagButton(text);
                tagDisplay.AddTag(newTagButton);
                
            }
        }

        //sets the image holder content
        private void SetImageHolderContent(ImageSource imageIn)
        {
            ImageButton newButton = new ImageButton() { Source = imageIn };
            this.imageHolder.Content = newButton;
            newButton.Clicked += (sen, e) => Navigation.PushAsync(new ImagePage(imageIn));
        }

        //asks the user to pic a photo
        public async void AttachPhoto()
        {
            if (await UserPermissions.ValidateStoragePermissions()) //check permissions
            {
                MediaFile picked = await ExternalResources.PickPhoto();
                FinalizePhoto(picked);

            }
        }

        //has the user take a photo
        public async void TakePhoto()
        {
            if (await UserPermissions.ValidateStoragePermissions() && await UserPermissions.ValidateCameraPermissions()) //check permissions
            {

                MediaFile taken = await ExternalResources.TakePhoto();
                FinalizePhoto(taken);
            }
        }

        //finalizes a selected photo. puts it in the right directory and display it on the page
        private void FinalizePhoto(MediaFile imageIn)
        {
            CachedImage loadingImage = new CachedImage()
            {
                Source = "loading"
            };

            if (imageIn != null)
            {
                this.imageHolder.Content = loadingImage;
                finalImage = imageIn;
                SetImageHolderContent(ImageTools.MediaFileToImageSource(imageIn));

            }



        }


        //refreshes the current page
        public void Refresh()
        {

            RefreshTags();
            if (finalImage == null)
            {
                this.imageHolder.Content = null;
            }
            else
            {
                SetImageHolderContent(ImageTools.MediaFileToImageSource(finalImage));
            }


        }

        //refreshes the current tags on the page
        private void RefreshTags()
        {
            tagDisplay.RefreshTags();
        }


        //confirms the new container
        public async void Confirm()
        {
  
            CreateContainer();
            if (finalImage != null)
            {
                await ImportPhoto(finalImage);
            }
            Finish();
        }

        //creates the container and add to database
        private void CreateContainer()
        {

            List<string> tagNamesList = new List<string>();
            foreach (TagButton button in tagDisplay.GetTags())
            {
                if (!tagNamesList.Contains(button.GetButtonText()))
                {

                    tagNamesList.Add(button.GetButtonText());
                }
                
            }
            if (isEditing) //if editing a previous container, change the value
            {
                previousContainer.name = nameInput.Text.Trim();
                previousContainer.tags = tagNamesList;
                previousContainer.notes = notesEditor.Text;

            }
            else
            {


                StorageContainer newContainer = new StorageContainer(newContainerId, location, nameInput.Text.Trim(), tagNamesList, notesEditor.Text);
                Console.WriteLine("tags");
                foreach (string tag in tagNamesList)
                {
                    Console.WriteLine(tag);
                }

                location.AddContainer(newContainer);

                DatabaseHandler.GetDatabase().AddContainer(newContainer);
            }

        }

        //finishes the container creation
        private async void Finish()
        {
            
            DataTools.Save(); //save data

           await  Navigation.PopAsync(); //go to location page once created

        }

        //compress the picture, move it to the correct directory and renames it
        //returns the new compressed and located image source
        private async Task<ImageSource> ImportPhoto(MediaFile photo)
        {

            ImageBaseHandler.current.AddLocationImage(newContainerId, ImageTools.MediaFileToImageSource(photo));
            await ImageTools.ImportImage(photo, Directories.containerImageDirectory, newContainerId.ToString());//save the new image in the internal storage with its id as the name
            return ImageTools.MediaFileToImageSource(finalImage);
        }

        //whether or not something can be deleted
        //returns true if any of the tags are highlighted
        public bool CanDelete()
        {

       
            return tagDisplay.CanDelete();
        }

        //deletes all the highlighted tags
        public void Delete()
        {
            tagDisplay.DeleteTags();
        }

    }
}